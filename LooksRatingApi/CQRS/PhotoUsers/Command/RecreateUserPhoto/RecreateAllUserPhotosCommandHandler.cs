using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.Services;
using LooksRatingApi.Services.PhotoProfiles;
using MediatR;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.RecreateUserPhoto
{
    public sealed class RecreateAllUserPhotosCommandHandler
        : IRequestHandler<RecreateAllUserPhotosCommand, Result<SetUserPhotoResult>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IRecreateAllUserPhotosValidator _validator;
        private readonly ISeasonRepository _seasonRepository;
        private readonly ICityService _cityService;
        private readonly INormalizeCityNameService _normalizeCityNameService;
        private readonly IPhotoProfileRatingResetService _ratingResetService;
        private readonly LooksRatingDbContext _context;

        public RecreateAllUserPhotosCommandHandler(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            IRecreateAllUserPhotosValidator validator,
            ISeasonRepository seasonRepository,
            ICityService cityService,
            INormalizeCityNameService normalizeCityNameService,
            IPhotoProfileRatingResetService ratingResetService,
            LooksRatingDbContext context)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
            _validator = validator;
            _seasonRepository = seasonRepository;
            _cityService = cityService;
            _normalizeCityNameService = normalizeCityNameService;
            _ratingResetService = ratingResetService;
            _context = context;
        }

        public async Task<Result<SetUserPhotoResult>> Handle(
            RecreateAllUserPhotosCommand command,
            CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<SetUserPhotoResult>(validationResult.Error);
            }

            var user = await _userRepository.GetUserByTelegramId(command.Request.TelegramId);
            if (user is null)
            {
                return Result.Failure<SetUserPhotoResult>(SetUserPhotoErrors.UserNotFound);
            }

            var season = await _seasonRepository.GetCurrent();
            if (season is null)
            {
                return Result.Failure<SetUserPhotoResult>(SetUserPhotoErrors.CurrentSeasonNotFound);
            }

            var profile = await _photoProfileRepository.GetByUserAndSeasonAsync(
                user.Id,
                season.Id,
                cancellationToken);
            if (profile is null || profile.Photos.Count == 0)
            {
                return Result.Failure<SetUserPhotoResult>(RecreateUserPhotoErrors.PhotoNotFound);
            }

            var nominationResult = await PhotoNominationResolver.ResolveAsync(
                user,
                command.Request.Nomination,
                _cityService,
                _normalizeCityNameService);
            if (nominationResult.IsFailure)
            {
                return Result.Failure<SetUserPhotoResult>(nominationResult.Error);
            }

            var (ageNomination, genderNomination, cityNomination) = nominationResult.Value;
            var previousNomination = PhotoProfileNomination.From(profile);
            var fileIds = command.Request.TelegramFileIds
                .Select(x => x?.Trim() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var sortedPhotos = profile.Photos.OrderBy(x => x.SortOrder).ToList();
            for (var i = 0; i < sortedPhotos.Count; i++)
            {
                if (i < fileIds.Count)
                {
                    sortedPhotos[i].TelegramFileId = fileIds[i];
                    continue;
                }

                profile.Photos.Remove(sortedPhotos[i]);
            }

            if (fileIds.Count > sortedPhotos.Count)
            {
                var nextSort = profile.Photos.Count == 0 ? 0 : profile.Photos.Max(x => x.SortOrder) + 1;
                for (var i = sortedPhotos.Count; i < fileIds.Count; i++)
                {
                    profile.Photos.Add(new Models.PhotoProfilePhoto
                    {
                        Id = Guid.NewGuid(),
                        PhotoProfileId = profile.Id,
                        TelegramFileId = fileIds[i],
                        SortOrder = nextSort++,
                        CreatedAt = DateTime.UtcNow,
                    });
                }
            }

            profile.CityNomination = cityNomination;
            profile.AgeNomination = ageNomination;
            profile.GenderNomination = genderNomination;
            profile.Status = Enums.StatusEnum.Active;

            var requestedNomination = PhotoProfileNomination.From(ageNomination, genderNomination, cityNomination.Value ?? string.Empty);
            IReadOnlyList<Guid>? reviewerUserIds = null;
            if (PhotoProfileRatingResetPolicy.ShouldResetRating(user.Status, previousNomination, requestedNomination))
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                reviewerUserIds = await _ratingResetService.ResetDatabaseAsync(profile, cancellationToken);
                await _photoProfileRepository.UpdateAsync(profile, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                await _ratingResetService.ResetCacheAsync(
                    profile,
                    previousNomination,
                    reviewerUserIds,
                    cancellationToken);
            }
            else
            {
                await _photoProfileRepository.UpdateAsync(profile, cancellationToken);
            }

            var primaryPhoto = profile.LegacyPhotoUsers.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            var first = profile.Photos.OrderBy(x => x.SortOrder).First();

            return Result.Success(new SetUserPhotoResult
            {
                ProfileId = profile.Id,
                PhotoUserId = primaryPhoto?.Id ?? Guid.Empty,
                UserId = user.Id,
                TelegramId = user.TelegramId,
                TelegramFileId = first.TelegramFileId,
                Rating = profile.Rating,
                RatingCount = profile.RatingCount,
                City = profile.CityNomination.Value ?? string.Empty,
            });
        }
    }
}
