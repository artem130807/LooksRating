using LooksRatingApi.CQRS.Seasons.Query.GetSeasonsByChapter;
using LooksRatingApi.Repositories;
using LooksRatingApi.Tests.Infrastructure.Builders;
using LooksRatingApi.Tests.Infrastructure.Fixtures;
using LooksRatingApi.Tests.Infrastructure.Helpers;

namespace LooksRatingApi.Tests.Integration.Cqrs;

[Collection(IntegrationCollection.Name)]
public sealed class GetSeasonsByChapterHandlerTests
{
    private readonly PostgresFixture _postgres;

    public GetSeasonsByChapterHandlerTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    [SkippableFact]
    public async Task Handle_ReturnsPhotoProfilesCount_NotPhotoCount()
    {
        IntegrationTestGuards.SkipUnlessDockerIsAvailable(_postgres);
        await using var context = _postgres.CreateContext();
        await DatabaseCleaner.ResetAsync(context);

        var (chapter, season) = await TestDataBuilder.SeedOpenSeasonAsync(context, seasonNumber: 3);
        var user1 = await TestDataBuilder.SeedUserAsync(context, 8001);
        var user2 = await TestDataBuilder.SeedUserAsync(context, 8002);
        await TestDataBuilder.SeedPhotoProfileAsync(context, user1, season, photoCount: 4);
        await TestDataBuilder.SeedPhotoProfileAsync(context, user2, season, photoCount: 2);

        var handler = new GetSeasonsByChapterHandler(
            new SeasonRepository(context),
            new ListSeasonsRepository(context),
            new PhotoProfileRepository(context));

        var result = await handler.Handle(
            new GetSeasonsByChapterQuery(chapter.Id, true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(summary => summary.Id == season.Id && summary.PhotoProfilesCount == 2);
    }
}
