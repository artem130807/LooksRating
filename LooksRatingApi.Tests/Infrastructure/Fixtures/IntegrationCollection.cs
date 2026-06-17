namespace LooksRatingApi.Tests.Infrastructure.Fixtures;

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<PostgresFixture>, ICollectionFixture<RedisFixture>
{
    public const string Name = "Integration";
}
