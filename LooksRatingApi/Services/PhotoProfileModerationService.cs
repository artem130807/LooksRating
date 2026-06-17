using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.UserTicketContracts;
using StackExchange.Redis;

namespace LooksRatingApi.Services
{
    public sealed class PhotoProfileModerationService : IPhotoProfileModerationService
    {
        private readonly IUserTicketRepository _userTicketRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly INormalizeCityNameService _normalizeCityNameService;
        private readonly IDatabase _redis;
        private readonly ILogger<PhotoProfileModerationService> _logger;

        public PhotoProfileModerationService(
            IUserTicketRepository userTicketRepository,
            IPhotoProfileRepository photoProfileRepository,
            IPhotoUserRepository photoUserRepository,
            INormalizeCityNameService normalizeCityNameService,
            IConnectionMultiplexer redis,
            ILogger<PhotoProfileModerationService> logger)
        {
            _userTicketRepository = userTicketRepository;
            _photoProfileRepository = photoProfileRepository;
            _photoUserRepository = photoUserRepository;
            _normalizeCityNameService = normalizeCityNameService;
            _redis = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<Result> DismissTicketAsync(Guid ticketId, long adminTelegramId, CancellationToken cancellationToken = default)
        {
            var ticket = await _userTicketRepository.GetTicketById(ticketId);
            if (ticket is null)
            {
                return Result.Failure("Жалоба не найдена");
            }

            await _userTicketRepository.Delete(ticketId);
            _logger.LogInformation(
                "Admin {AdminTelegramId} dismissed ticket {TicketId} for profile {ProfileId}",
                adminTelegramId,
                ticketId,
                ticket.PhotoProfileId);

            return Result.Success();
        }

        public async Task<Result> DeleteReportedProfileAsync(Guid ticketId, long adminTelegramId, CancellationToken cancellationToken = default)
        {
            var ticket = await _userTicketRepository.GetTicketById(ticketId);
            if (ticket is null)
            {
                return Result.Failure("Жалоба не найдена");
            }

            var profile = await _photoProfileRepository.GetByIdAsync(ticket.PhotoProfileId, cancellationToken);
            if (profile is null)
            {
                await _userTicketRepository.Delete(ticketId);
                return Result.Success();
            }

            var cityKey = _normalizeCityNameService.Normalize(profile.CityNomination.Value ?? string.Empty);
            var ratingKey = PhotoRedisKeys.RatingSortedSet(cityKey, profile.SeasonId);
            await _redis.SortedSetRemoveAsync(ratingKey, profile.Id.ToString());
            await _redis.KeyDeleteAsync(PhotoRedisKeys.ProfileHash(profile.Id));

            var legacyPhotoUsers = await _photoUserRepository.GetByProfileIdAsync(profile.Id, cancellationToken);
            foreach (var legacyPhotoUser in legacyPhotoUsers)
            {
                await _photoUserRepository.Delete(legacyPhotoUser.Id);
            }

            await _photoProfileRepository.DeleteAsync(profile.Id, cancellationToken);

            _logger.LogInformation(
                "Admin {AdminTelegramId} deleted profile {ProfileId} from ticket {TicketId}",
                adminTelegramId,
                profile.Id,
                ticketId);

            return Result.Success();
        }
    }
}
