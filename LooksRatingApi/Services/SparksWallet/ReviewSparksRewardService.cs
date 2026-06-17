using LooksRatingApi.Contracts.SparksLedgerContracts;
using StackExchange.Redis;

namespace LooksRatingApi.Services.SparksLedger
{
    public sealed class ReviewSparksRewardService : IReviewSparksRewardService
    {
        private const int DailyReviewSparksLimit = 20;
        private const decimal ReviewSparksReward = 1m;
        private static readonly TimeSpan DailyCounterTtl = TimeSpan.FromHours(24);

        private readonly ICurrencySparksService _currencySparksService;
        private readonly ISparksWalletProvisioner _sparksWalletProvisioner;
        private readonly IDatabase _redis;
        private readonly ILogger<ReviewSparksRewardService> _logger;

        public ReviewSparksRewardService(
            ICurrencySparksService currencySparksService,
            ISparksWalletProvisioner sparksWalletProvisioner,
            IConnectionMultiplexer multiplexer,
            ILogger<ReviewSparksRewardService> logger)
        {
            _currencySparksService = currencySparksService;
            _sparksWalletProvisioner = sparksWalletProvisioner;
            _redis = multiplexer.GetDatabase();
            _logger = logger;
        }

        public async Task TryAwardForReviewAsync(
            long reviewerTelegramId,
            Guid reviewerUserId,
            CancellationToken cancellationToken = default)
        {
            if (reviewerTelegramId <= 0 || reviewerUserId == Guid.Empty)
            {
                return;
            }

            var counterKey = BuildDailyCounterKey(reviewerTelegramId);

            try
            {
                if (!await _redis.KeyExistsAsync(counterKey))
                {
                    await _redis.StringSetAsync(counterKey, 0, DailyCounterTtl);
                }

                var awardedToday = (int)await _redis.StringGetAsync(counterKey);
                if (awardedToday >= DailyReviewSparksLimit)
                {
                    return;
                }

                await _sparksWalletProvisioner.EnsureForUserAsync(reviewerUserId, cancellationToken);
                await _currencySparksService.Credited(reviewerUserId, ReviewSparksReward, cancellationToken);
                await _redis.StringIncrementAsync(counterKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to award review sparks for user {UserId} (telegram {TelegramId})",
                    reviewerUserId,
                    reviewerTelegramId);
            }
        }

        private static string BuildDailyCounterKey(long reviewerTelegramId) =>
            $"count_review_{reviewerTelegramId}";
    }
}
