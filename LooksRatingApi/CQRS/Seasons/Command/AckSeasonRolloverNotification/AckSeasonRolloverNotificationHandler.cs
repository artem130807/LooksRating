using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.Services.SeasonLifecycle;
using MediatR;

namespace LooksRatingApi.CQRS.Seasons.Command.AckSeasonRolloverNotification
{
    public sealed class AckSeasonRolloverNotificationHandler
        : IRequestHandler<AckSeasonRolloverNotificationCommand, Result<string>>
    {
        private readonly ISeasonRolloverNotificationStore _store;

        public AckSeasonRolloverNotificationHandler(ISeasonRolloverNotificationStore store)
        {
            _store = store;
        }

        public async Task<Result<string>> Handle(
            AckSeasonRolloverNotificationCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.EventId)
                || !SeasonRolloverEventId.TryParse(request.EventId, out _, out _))
            {
                return Result.Failure<string>("SeasonRolloverNotificationNotFound");
            }

            if (request.RecipientTelegramIds.Count == 0)
            {
                return Result.Failure<string>("SeasonRolloverRecipientsRequired");
            }

            await _store.AckDeliveredAsync(
                request.EventId,
                request.RecipientTelegramIds,
                cancellationToken);

            return Result.Success("ok");
        }
    }
}
