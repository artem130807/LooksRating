using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.Infrastructure.SeasonNotifications;
using MediatR;
using Microsoft.Extensions.Options;

namespace LooksRatingApi.CQRS.Seasons.Query.GetPendingSeasonRolloverNotifications
{
    public sealed class GetPendingSeasonRolloverNotificationsHandler
        : IRequestHandler<GetPendingSeasonRolloverNotificationsQuery, IReadOnlyList<PendingSeasonRolloverNotificationResponse>>
    {
        private readonly ISeasonRolloverNotificationStore _store;
        private readonly SeasonRolloverNotificationOptions _options;

        public GetPendingSeasonRolloverNotificationsHandler(
            ISeasonRolloverNotificationStore store,
            IOptions<SeasonRolloverNotificationOptions> options)
        {
            _store = store;
            _options = options.Value;
        }

        public async Task<IReadOnlyList<PendingSeasonRolloverNotificationResponse>> Handle(
            GetPendingSeasonRolloverNotificationsQuery request,
            CancellationToken cancellationToken)
        {
            if (!_options.Enabled)
            {
                return Array.Empty<PendingSeasonRolloverNotificationResponse>();
            }

            var limit = request.Limit > 0
                ? Math.Min(request.Limit, _options.PendingBatchSize)
                : _options.PendingBatchSize;

            var pending = await _store.GetPendingBatchesAsync(limit, cancellationToken);
            return pending
                .Select(batch => new PendingSeasonRolloverNotificationResponse
                {
                    EventId = batch.EventId,
                    ClosedSeasonId = batch.ClosedSeasonId,
                    ClosedSeasonName = batch.ClosedSeasonName,
                    ClosedSeasonNumber = batch.ClosedSeasonNumber,
                    NewSeasonId = batch.NewSeasonId,
                    NewSeasonName = batch.NewSeasonName,
                    NewSeasonNumber = batch.NewSeasonNumber,
                    RecipientTelegramIds = batch.RecipientTelegramIds
                })
                .ToList();
        }
    }
}
