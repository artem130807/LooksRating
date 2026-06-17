using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto
{
    public sealed class SetUserPhotoCommandHandler : IRequestHandler<SetUserPhotoCommand, Result<SetUserPhotoResult>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ISetUserPhotoValidator _validator;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IPhotoUserLifecycleService _photoUserLifecycleService;
        private readonly ICityService _cityService;
        private readonly INormalizeCityNameService _normalizeCityNameService;

        public SetUserPhotoCommandHandler(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            ISetUserPhotoValidator validator,
            ISeasonRepository seasonRepository,
            IPhotoUserLifecycleService photoUserLifecycleService,
            ICityService cityService,
            INormalizeCityNameService normalizeCityNameService)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
            _validator = validator;
            _seasonRepository = seasonRepository;
            _photoUserLifecycleService = photoUserLifecycleService;
            _cityService = cityService;
            _normalizeCityNameService = normalizeCityNameService;
        }

        public async Task<Result<SetUserPhotoResult>> Handle(SetUserPhotoCommand query, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(query, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<SetUserPhotoResult>(validationResult.Error);
            }

            var user = await _userRepository.GetUserByTelegramId(query.request.TelegramId);
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
            if (profile is not null && user.Status == Enums.VipStatus.Unavaillable)
            {
                return Result.Failure<SetUserPhotoResult>(SetUserPhotoErrors.PhotoAlreadyExists);
            }
            var isWithinVipPhotoLimit = await _photoProfileRepository.IsWithinVipPhotoLimitAsync(season.Id, query.request.TelegramId, cancellationToken);
            if(isWithinVipPhotoLimit == false)
            {
                return Result.Failure<SetUserPhotoResult>(SetUserPhotoErrors.VipPhotoLimitExceeded);
            }
            var nominationResult = await PhotoNominationResolver.ResolveAsync(
                user,
                query.request.Nomination,
                _cityService,
                _normalizeCityNameService);
            if (nominationResult.IsFailure)
            {
                return Result.Failure<SetUserPhotoResult>(nominationResult.Error);
            }

            var (ageNomination, genderNomination, cityNomination) = nominationResult.Value;
            var telegramFileId = query.request.TelegramFileId.Trim();
            PhotoUser photoUser;
            try
            {
                photoUser = await _photoUserLifecycleService.CreateAsync(
                    user,
                    telegramFileId,
                    season,
                    ageNomination,
                    genderNomination,
                    cityNomination,
                    cancellationToken);
            }
            catch (InvalidOperationException ex) when (ex.Message is SetUserPhotoErrors.PhotoUploadInProgress
                or SetUserPhotoErrors.VipPhotoLimitExceeded)
            {
                return Result.Failure<SetUserPhotoResult>(ex.Message);
            }

            return Result.Success(new SetUserPhotoResult
            {
                ProfileId = photoUser.PhotoProfileId ?? Guid.Empty,
                PhotoUserId = photoUser.Id,
                UserId = user.Id,
                TelegramId = user.TelegramId,
                TelegramFileId = photoUser.TelegramFileId,
                Rating = photoUser.Rating,
                RatingCount = photoUser.RatingCount,
                City = photoUser.CityNomination.Value ?? string.Empty,
            });
        }
    }
}
