using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonLifecycle;
using LooksRatingApi.Enums;
using LooksRatingApi.Infrastructure.SeasonNotifications;
using LooksRatingApi.Models;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.SeasonLifecycle;
using LooksRatingApi.Tests.Infrastructure.Builders;
using LooksRatingApi.Tests.Infrastructure.Fixtures;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LooksRatingApi.Tests.Unit.Services.SeasonLifecycle;

public sealed class SeasonRolloverNotifierTests
{
    [Fact]
    public async Task EnqueueForRolloverAsync_EnqueuesParticipantsInBatches()
    {
        var closedSeason = Season.Create("Потный июнь", 6, Guid.NewGuid()).Value;
        var newSeason = Season.Create("Обгоревший июль", 7, Guid.NewGuid()).Value;
        var photoProfiles = Substitute.For<IPhotoProfileRepository>();
        var store = Substitute.For<ISeasonRolloverNotificationStore>();
        var options = Options.Create(new SeasonRolloverNotificationOptions
        {
            Enabled = true,
            EnqueueBatchSize = 2,
            TtlDays = 45
        });

        photoProfiles
            .GetParticipantTelegramIdsBatchAsync(closedSeason.Id, 0, 2, Arg.Any<CancellationToken>())
            .Returns(new List<long> { 1001, 1002 });
        photoProfiles
            .GetParticipantTelegramIdsBatchAsync(closedSeason.Id, 2, 2, Arg.Any<CancellationToken>())
            .Returns(new List<long> { 1003 });
        photoProfiles
            .GetParticipantTelegramIdsBatchAsync(closedSeason.Id, 4, 2, Arg.Any<CancellationToken>())
            .Returns(new List<long>());

        store.TryEnqueueBatchAsync(Arg.Any<SeasonRolloverEnqueueRequest>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(2, 1);

        var notifier = new SeasonRolloverNotifier(
            photoProfiles,
            store,
            options,
            NullLogger<SeasonRolloverNotifier>.Instance);

        var total = await notifier.EnqueueForRolloverAsync(closedSeason, newSeason);

        total.Should().Be(3);
        await store.Received(2).TryEnqueueBatchAsync(
            Arg.Is<SeasonRolloverEnqueueRequest>(x =>
                x.ClosedSeasonId == closedSeason.Id
                && x.NewSeasonId == newSeason.Id
                && x.RecipientTelegramIds.Count > 0),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnqueueForRolloverAsync_WhenDisabled_ReturnsZero()
    {
        var closedSeason = Season.Create("Потный июнь", 6, Guid.NewGuid()).Value;
        var newSeason = Season.Create("Обгоревший июль", 7, Guid.NewGuid()).Value;
        var photoProfiles = Substitute.For<IPhotoProfileRepository>();
        var store = Substitute.For<ISeasonRolloverNotificationStore>();
        var options = Options.Create(new SeasonRolloverNotificationOptions { Enabled = false });

        var notifier = new SeasonRolloverNotifier(
            photoProfiles,
            store,
            options,
            NullLogger<SeasonRolloverNotifier>.Instance);

        var total = await notifier.EnqueueForRolloverAsync(closedSeason, newSeason);

        total.Should().Be(0);
        await photoProfiles.DidNotReceive().GetParticipantTelegramIdsBatchAsync(
            Arg.Any<Guid>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }
}

[Collection(IntegrationCollection.Name)]
public sealed class PhotoProfileRepositoryParticipantTelegramIdsTests
{
    private readonly PostgresFixture _postgres;

    public PhotoProfileRepositoryParticipantTelegramIdsTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [SkippableFact]
    public async Task GetParticipantTelegramIdsBatchAsync_ReturnsDistinctActiveAndArchived()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (_, season) = await TestDataBuilder.SeedOpenSeasonAsync(context);
        var activeUser = await TestDataBuilder.SeedUserAsync(context, 9101);
        var archivedUser = await TestDataBuilder.SeedUserAsync(context, 9102);
        var rejectedUser = await TestDataBuilder.SeedUserAsync(context, 9103);
        var duplicateUser = await TestDataBuilder.SeedUserAsync(context, 9104);

        await TestDataBuilder.SeedPhotoProfileAsync(context, activeUser, season, StatusEnum.Active, photoCount: 2);
        await TestDataBuilder.SeedPhotoProfileAsync(context, archivedUser, season, StatusEnum.Archived);
        await TestDataBuilder.SeedPhotoProfileAsync(context, rejectedUser, season, StatusEnum.Rejected);
        await TestDataBuilder.SeedPhotoProfileAsync(context, duplicateUser, season, StatusEnum.Active);

        var repository = new PhotoProfileRepository(context);
        var ids = await repository.GetParticipantTelegramIdsBatchAsync(season.Id, skip: 0, take: 10);

        ids.Should().BeEquivalentTo(new long[] { 9101, 9102, 9104 }, options => options.WithStrictOrdering());
    }
}
