using LooksRatingApi.Contracts.SparksLedgerContracts;
using StackExchange.Redis;

namespace LooksRatingApi.Services.SparksLedger
{
    public sealed class RatedProfileSparksRewardService : IRatedProfileSparksRewardService
    {
        private const int DailyRatedProfileSparksLimit = 15;
        private const decimal RatedProfileSparksReward = 0.5m;
        private static readonly TimeSpan DailyCounterTtl = TimeSpan.FromHours(24);

        private readonly ICurrencySparksService _currencySparksService;
        private readonly ISparksWalletProvisioner _sparksWalletProvisioner;
        private readonly IDatabase _redis;
        private readonly ILogger<RatedProfileSparksRewardService> _logger;

        public RatedProfileSparksRewardService(
            ICurrencySparksService currencySparksService,
            ISparksWalletProvisioner sparksWalletProvisioner,
            IConnectionMultiplexer multiplexer,
            ILogger<RatedProfileSparksRewardService> logger)
        {
            _currencySparksService = currencySparksService;
            _sparksWalletProvisioner = sparksWalletProvisioner;
            _redis = multiplexer.GetDatabase();
            _logger = logger;
        }

        public async Task<bool> TryAwardForRatedProfileAsync(
            long ratedUserTelegramId,
            Guid ratedUserId,
            CancellationToken cancellationToken = default)
        {
            if (ratedUserTelegramId <= 0 || ratedUserId == Guid.Empty)
            {
                return true;
            }

            var counterKey = BuildDailyCounterKey(ratedUserTelegramId);

            try
            {
                if (!await _redis.KeyExistsAsync(counterKey))
                {
                    await _redis.StringSetAsync(counterKey, 0, DailyCounterTtl);
                }

                var awardedToday = (int)await _redis.StringGetAsync(counterKey);
                if (awardedToday >= DailyRatedProfileSparksLimit)
                {
                    return true;
                }

                await _sparksWalletProvisioner.EnsureForUserAsync(ratedUserId, cancellationToken);
                await _currencySparksService.Credited(
                    ratedUserId,
                    RatedProfileSparksReward,
                    cancellationToken);
                await _redis.StringIncrementAsync(counterKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to award rated-profile sparks for user {UserId} (telegram {TelegramId})",
                    ratedUserId,
                    ratedUserTelegramId);
                return false;
            }
        }

        private static string BuildDailyCounterKey(long ratedUserTelegramId) =>
            $"count_rated_profile_{ratedUserTelegramId}";
    }
}
