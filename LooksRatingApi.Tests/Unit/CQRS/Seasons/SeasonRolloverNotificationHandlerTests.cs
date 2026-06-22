using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.CQRS.Seasons.Command.AckSeasonRolloverNotification;
using LooksRatingApi.CQRS.Seasons.Query.GetPendingSeasonRolloverNotifications;
using LooksRatingApi.Infrastructure.SeasonNotifications;
using LooksRatingApi.Services.SeasonLifecycle;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LooksRatingApi.Tests.Unit.CQRS.Seasons;

public sealed class GetPendingSeasonRolloverNotificationsHandlerTests
{
    [Fact]
    public async Task Handle_WhenDisabled_ReturnsEmptyList()
    {
        var store = Substitute.For<ISeasonRolloverNotificationStore>();
        var handler = new GetPendingSeasonRolloverNotificationsHandler(
            store,
            Options.Create(new SeasonRolloverNotificationOptions { Enabled = false }));

        var result = await handler.Handle(new GetPendingSeasonRolloverNotificationsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
        await store.DidNotReceive().GetPendingBatchesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsMappedPendingBatch()
    {
        var closedId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        var eventId = SeasonRolloverEventId.Create(closedId, newId);
        var store = Substitute.For<ISeasonRolloverNotificationStore>();
        store.GetPendingBatchesAsync(50, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new SeasonRolloverPendingBatch
                {
                    EventId = eventId,
                    ClosedSeasonId = closedId,
                    ClosedSeasonName = "Потный июнь",
                    ClosedSeasonNumber = 6,
                    NewSeasonId = newId,
                    NewSeasonName = "Обгоревший июль",
                    NewSeasonNumber = 7,
                    RecipientTelegramIds = new long[] { 1001, 1002 }
                }
            });

        var handler = new GetPendingSeasonRolloverNotificationsHandler(
            store,
            Options.Create(new SeasonRolloverNotificationOptions { Enabled = true, PendingBatchSize = 50 }));

        var result = await handler.Handle(new GetPendingSeasonRolloverNotificationsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].EventId.Should().Be(eventId);
        result[0].RecipientTelegramIds.Should().BeEquivalentTo(new long[] { 1001, 1002 });
    }
}

public sealed class AckSeasonRolloverNotificationHandlerTests
{
    [Fact]
    public async Task Handle_WithInvalidEventId_ReturnsNotFound()
    {
        var store = Substitute.For<ISeasonRolloverNotificationStore>();
        var handler = new AckSeasonRolloverNotificationHandler(store);

        var result = await handler.Handle(
            new AckSeasonRolloverNotificationCommand("invalid", new long[] { 1 }),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("SeasonRolloverNotificationNotFound");
    }

    [Fact]
    public async Task Handle_WithRecipients_AcksStore()
    {
        var closedId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        var eventId = SeasonRolloverEventId.Create(closedId, newId);
        var store = Substitute.For<ISeasonRolloverNotificationStore>();
        var handler = new AckSeasonRolloverNotificationHandler(store);

        var result = await handler.Handle(
            new AckSeasonRolloverNotificationCommand(eventId, new long[] { 1001 }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await store.Received(1).AckDeliveredAsync(eventId, Arg.Any<IReadOnlyList<long>>(), Arg.Any<CancellationToken>());
    }
}
