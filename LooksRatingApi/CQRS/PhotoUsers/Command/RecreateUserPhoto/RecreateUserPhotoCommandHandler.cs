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
    public sealed class RecreateUserPhotoCommandHandler
        : IRequestHandler<RecreateUserPhotoCommand, Result<SetUserPhotoResult>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IRecreateUserPhotoValidator _validator;
        private readonly ISeasonRepository _seasonRepository;
        private readonly ICityService _cityService;
        private readonly INormalizeCityNameService _normalizeCityNameService;
        private readonly IPhotoProfileRatingResetService _ratingResetService;
        private readonly LooksRatingDbContext _context;

        public RecreateUserPhotoCommandHandler(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            IRecreateUserPhotoValidator validator,
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
            RecreateUserPhotoCommand command,
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
            var telegramFileId = command.Request.TelegramFileId.Trim();
            var target = command.Request.TargetPhotoId.HasValue
                ? profile.Photos.FirstOrDefault(x => x.Id == command.Request.TargetPhotoId.Value)
                : profile.Photos.OrderBy(x => x.SortOrder).FirstOrDefault();
            if (target is null)
            {
                return Result.Failure<SetUserPhotoResult>(RecreateUserPhotoErrors.TargetPhotoNotFound);
            }

            target.TelegramFileId = telegramFileId;
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

            return Result.Success(new SetUserPhotoResult
            {
                ProfileId = profile.Id,
                PhotoUserId = primaryPhoto?.Id ?? Guid.Empty,
                UserId = user.Id,
                TelegramId = user.TelegramId,
                TelegramFileId = target.TelegramFileId,
                Rating = profile.Rating,
                RatingCount = profile.RatingCount,
                City = profile.CityNomination.Value ?? string.Empty,
            });
        }
    }
}
