using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.Infrastructure.SeasonNotifications;
using LooksRatingApi.Models;
using LooksRatingApi.Services.SeasonLifecycle;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.Services.SeasonLifecycle
{
    public sealed class SeasonRolloverNotifier : ISeasonRolloverNotifier
    {
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ISeasonRolloverNotificationStore _notificationStore;
        private readonly SeasonRolloverNotificationOptions _options;
        private readonly ILogger<SeasonRolloverNotifier> _logger;

        public SeasonRolloverNotifier(
            IPhotoProfileRepository photoProfileRepository,
            ISeasonRolloverNotificationStore notificationStore,
            IOptions<SeasonRolloverNotificationOptions> options,
            ILogger<SeasonRolloverNotifier> logger)
        {
            _photoProfileRepository = photoProfileRepository;
            _notificationStore = notificationStore;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<int> EnqueueForRolloverAsync(
            Season closedSeason,
            Season newSeason,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                return 0;
            }

            var batchSize = Math.Max(1, _options.EnqueueBatchSize);
            var ttl = TimeSpan.FromDays(Math.Max(1, _options.TtlDays));
            var skip = 0;
            var totalEnqueued = 0;

            while (true)
            {
                var telegramIds = await _photoProfileRepository.GetParticipantTelegramIdsBatchAsync(
                    closedSeason.Id,
                    skip,
                    batchSize,
                    cancellationToken);

                if (telegramIds.Count == 0)
                {
                    break;
                }

                var enqueued = await _notificationStore.TryEnqueueBatchAsync(
                    new SeasonRolloverEnqueueRequest
                    {
                        ClosedSeasonId = closedSeason.Id,
                        ClosedSeasonName = closedSeason.Name,
                        ClosedSeasonNumber = closedSeason.Number,
                        NewSeasonId = newSeason.Id,
                        NewSeasonName = newSeason.Name,
                        NewSeasonNumber = newSeason.Number,
                        RecipientTelegramIds = telegramIds
                    },
                    ttl,
                    cancellationToken);

                if (enqueued == 0 && telegramIds.Count > 0)
                {
                    enqueued = await _notificationStore.TryEnqueueBatchAsync(
                        new SeasonRolloverEnqueueRequest
                        {
                            ClosedSeasonId = closedSeason.Id,
                            ClosedSeasonName = closedSeason.Name,
                            ClosedSeasonNumber = closedSeason.Number,
                            NewSeasonId = newSeason.Id,
                            NewSeasonName = newSeason.Name,
                            NewSeasonNumber = newSeason.Number,
                            RecipientTelegramIds = telegramIds
                        },
                        ttl,
                        cancellationToken);
                }

                totalEnqueued += enqueued;
                if (enqueued == 0 && telegramIds.Count > 0)
                {
                    _logger.LogWarning(
                        "Season rollover enqueue batch returned 0 for closed={ClosedSeasonId}, skip={Skip}, batchSize={BatchSize}",
                        closedSeason.Id,
                        skip,
                        telegramIds.Count);
                }

                skip += batchSize;

                if (telegramIds.Count < batchSize)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Season rollover notifications queued: closed={ClosedSeasonId}, new={NewSeasonId}, enqueued={Enqueued}",
                closedSeason.Id,
                newSeason.Id,
                totalEnqueued);

            return totalEnqueued;
        }
    }
}
