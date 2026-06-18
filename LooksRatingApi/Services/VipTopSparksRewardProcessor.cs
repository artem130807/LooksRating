using LooksRatingApi.Contracts;
using LooksRatingApi.Infrastructure.DistributedLock;
using LooksRatingApi.Infrastructure.Quartz;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Services
{
    public sealed class VipTopSparksRewardProcessor : IVipTopSparksRewardProcessor
    {
        private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(30);

        private readonly IVipTopCategoryService _vipTopCategoryService;
        private readonly IVipStatusExtensionService _vipStatusExtensionService;
        private readonly ISparksRewardCreditingService _sparksRewardCreditingService;
        private readonly IRedisDistributedLock _distributedLock;
        private readonly ApplicationClock _clock;
        private readonly ILogger<VipTopSparksRewardProcessor> _logger;

        public VipTopSparksRewardProcessor(
            IVipTopCategoryService vipTopCategoryService,
            IVipStatusExtensionService vipStatusExtensionService,
            ISparksRewardCreditingService sparksRewardCreditingService,
            IRedisDistributedLock distributedLock,
            ApplicationClock clock,
            ILogger<VipTopSparksRewardProcessor> logger)
        {
            _vipTopCategoryService = vipTopCategoryService;
            _vipStatusExtensionService = vipStatusExtensionService;
            _sparksRewardCreditingService = sparksRewardCreditingService;
            _distributedLock = distributedLock;
            _clock = clock;
            _logger = logger;
        }

        public async Task<VipTopSparksRewardResult> ProcessAsync(CancellationToken cancellationToken = default)
        {
            var applicationNow = _clock.GetNow();
            if (!VipTopRewardSchedule.IsRewardDay(applicationNow))
            {
                _logger.LogInformation(
                    "VIP top sparks reward skipped: not a biweekly reward day (next={NextRewardDay:yyyy-MM-dd})",
                    VipTopRewardSchedule.GetNextRewardDay(applicationNow));
                return new VipTopSparksRewardResult(0, 0, 0, 0, 0, 0, 0);
            }

            await using var lockHandle = await _distributedLock.TryAcquireAsync(
                DistributedLockKeys.VipTopSparksReward,
                LockTtl,
                cancellationToken);

            if (lockHandle is null)
            {
                _logger.LogInformation("VIP top sparks reward skipped: lock is held by another instance");
                return new VipTopSparksRewardResult(0, 0, 0, 0, 0, 0, 0);
            }

            var categories = await _vipTopCategoryService.GetQualifiedCategoriesAsync(cancellationToken);
            if (categories.Count == 0)
            {
                _logger.LogInformation("VIP top sparks reward: no qualified categories");
                return new VipTopSparksRewardResult(0, 0, 0, 0, 0, 0, 0);
            }

            var seasonIds = categories.Select(category => category.SeasonId).Distinct().ToList();
            if (seasonIds.Count != 1)
            {
                _logger.LogError(
                    "VIP top sparks reward aborted: expected one season, got {SeasonCount}",
                    seasonIds.Count);
                return new VipTopSparksRewardResult(0, 0, 0, 0, 0, 0, 0);
            }

            var seasonId = seasonIds[0];
            var periodKey = VipTopRewardPeriod.BuildKey(seasonId, applicationNow);

            _logger.LogInformation(
                "VIP top sparks reward started: period={PeriodKey}, tz={TimeZone}, localNow={LocalNow:O}",
                periodKey,
                _clock.TimeZone.Id,
                applicationNow);

            var sparksResult = await CreditSparksRewardsAsync(
                categories,
                periodKey,
                cancellationToken);

            var extensionTelegramIds = VipTopPlacement.GetExtensionTelegramIds(categories);
            var extensionResult = await _vipStatusExtensionService.ExtendByTelegramIdsAsync(
                extensionTelegramIds,
                seasonId,
                cancellationToken);

            _logger.LogInformation(
                "VIP top biweekly rewards period={PeriodKey}: sparks credited={Credited}, sparks skipped={SparksSkipped}, sparks notFound={SparksNotFound}, sparks failed={SparksFailed}, vip extended={VipExtended}, vip skipped={VipSkipped}, vip notFound={VipNotFound}",
                periodKey,
                sparksResult.Credited,
                sparksResult.Skipped,
                sparksResult.NotFound,
                sparksResult.Failed,
                extensionResult.Extended,
                extensionResult.Skipped,
                extensionResult.NotFound);

            return new VipTopSparksRewardResult(
                sparksResult.Credited,
                sparksResult.Skipped,
                sparksResult.NotFound,
                sparksResult.Failed,
                extensionResult.Extended,
                extensionResult.Skipped,
                extensionResult.NotFound);
        }

        private async Task<SparksRewardCreditingResult> CreditSparksRewardsAsync(
            IReadOnlyList<VipTopCategory> categories,
            string periodKey,
            CancellationToken cancellationToken)
        {
            var placementRecipients = VipTopPlacement.GetSparksRewardRecipients(categories);
            if (placementRecipients.Count == 0)
            {
                return new SparksRewardCreditingResult(0, 0, 0, 0);
            }

            var recipients = placementRecipients
                .Select(recipient => new SparksRewardRecipient(
                    recipient.TelegramId,
                    recipient.Place,
                    recipient.SparksAmount,
                    VipTopRewardPeriod.BuildSparksPayload(
                        periodKey,
                        recipient.Place,
                        recipient.TelegramId,
                        recipient.CategoryFingerprint)))
                .ToList();

            return await _sparksRewardCreditingService.CreditAsync(
                recipients,
                VipTopRules.VipProductCode,
                "vip-top",
                cancellationToken);
        }
    }
}
