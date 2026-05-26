using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.RecreateUserPhoto
{
    public sealed class RecreateUserPhotoCommandHandler
        : IRequestHandler<RecreateUserPhotoCommand, Result<SetUserPhotoResult>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly IRecreateUserPhotoValidator _validator;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IPhotoUserLifecycleService _photoUserLifecycleService;
        private readonly ICityService _cityService;
        private readonly INormalizeCityNameService _normalizeCityNameService;

        public RecreateUserPhotoCommandHandler(
            IUserRepository userRepository,
            IPhotoUserRepository photoUserRepository,
            IRecreateUserPhotoValidator validator,
            ISeasonRepository seasonRepository,
            IPhotoUserLifecycleService photoUserLifecycleService,
            ICityService cityService,
            INormalizeCityNameService normalizeCityNameService)
        {
            _userRepository = userRepository;
            _photoUserRepository = photoUserRepository;
            _validator = validator;
            _seasonRepository = seasonRepository;
            _photoUserLifecycleService = photoUserLifecycleService;
            _cityService = cityService;
            _normalizeCityNameService = normalizeCityNameService;
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

            var existingPhoto = await _photoUserRepository.GetByTelegramIdAndSeasonIdAsync(
                command.Request.TelegramId,
                season.Id,
                cancellationToken);
            if (existingPhoto is null)
            {
                return Result.Failure<SetUserPhotoResult>(RecreateUserPhotoErrors.PhotoNotFound);
            }

            await _photoUserLifecycleService.RemoveAsync(existingPhoto, season, cancellationToken);

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
            var telegramFileId = command.Request.TelegramFileId.Trim();
            var photoUser = await _photoUserLifecycleService.CreateAsync(
                user,
                telegramFileId,
                season,
                ageNomination,
                genderNomination,
                cityNomination,
                cancellationToken);

            return Result.Success(new SetUserPhotoResult
            {
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
