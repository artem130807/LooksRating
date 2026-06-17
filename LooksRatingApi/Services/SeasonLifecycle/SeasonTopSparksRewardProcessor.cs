using LooksRatingApi.Contracts;

namespace LooksRatingApi.Services.SeasonLifecycle
{
    public sealed class SeasonTopSparksRewardProcessor : ISeasonTopSparksRewardProcessor
    {
        private readonly ISeasonTopCategoryService _seasonTopCategoryService;
        private readonly ISparksRewardCreditingService _sparksRewardCreditingService;
        private readonly ILogger<SeasonTopSparksRewardProcessor> _logger;

        public SeasonTopSparksRewardProcessor(
            ISeasonTopCategoryService seasonTopCategoryService,
            ISparksRewardCreditingService sparksRewardCreditingService,
            ILogger<SeasonTopSparksRewardProcessor> logger)
        {
            _seasonTopCategoryService = seasonTopCategoryService;
            _sparksRewardCreditingService = sparksRewardCreditingService;
            _logger = logger;
        }

        public async Task<SeasonTopSparksRewardResult> ProcessForSeasonAsync(
            Guid seasonId,
            bool seasonIsClosed,
            CancellationToken cancellationToken = default)
        {
            var categories = await _seasonTopCategoryService.GetQualifiedCategoriesForSeasonAsync(
                seasonId,
                seasonIsClosed,
                cancellationToken);

            if (categories.Count == 0)
            {
                _logger.LogInformation(
                    "Season top sparks reward skipped: no qualified categories for season {SeasonId}",
                    seasonId);
                return new SeasonTopSparksRewardResult(0, 0, 0, 0);
            }

            var periodKey = SeasonTopRewardPeriod.BuildKey(seasonId);
            var placementRecipients = SeasonTopPlacement.GetSparksRewardRecipients(categories);
            var recipients = placementRecipients
                .Select(recipient => new SparksRewardRecipient(
                    recipient.TelegramId,
                    recipient.Place,
                    recipient.SparksAmount,
                    SeasonTopRewardPeriod.BuildSparksPayload(
                        periodKey,
                        recipient.Place,
                        recipient.TelegramId,
                        recipient.CategoryFingerprint)))
                .ToList();

            _logger.LogInformation(
                "Season top sparks reward started: season={SeasonId}, period={PeriodKey}, recipients={RecipientCount}",
                seasonId,
                periodKey,
                recipients.Count);

            var result = await _sparksRewardCreditingService.CreditAsync(
                recipients,
                SeasonTopRules.RewardProductCode,
                "season-top",
                cancellationToken);

            _logger.LogInformation(
                "Season top sparks reward finished: season={SeasonId}, credited={Credited}, skipped={Skipped}, notFound={NotFound}, failed={Failed}",
                seasonId,
                result.Credited,
                result.Skipped,
                result.NotFound,
                result.Failed);

            return new SeasonTopSparksRewardResult(
                result.Credited,
                result.Skipped,
                result.NotFound,
                result.Failed);
        }
    }
}
