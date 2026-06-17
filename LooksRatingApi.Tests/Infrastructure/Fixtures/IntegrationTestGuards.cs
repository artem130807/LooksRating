using Xunit;
using Xunit.Sdk;

namespace LooksRatingApi.Tests.Infrastructure.Fixtures;

internal static class IntegrationTestGuards
{
    private static bool IntegrationRequired =>
        string.Equals(
            Environment.GetEnvironmentVariable("LOOKSRATING_INTEGRATION_TESTS_REQUIRED"),
            "true",
            StringComparison.OrdinalIgnoreCase)
        || string.Equals(
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public static void SkipUnlessDockerIsAvailable(PostgresFixture postgres, RedisFixture? redis = null)
    {
        EnsureAvailable("PostgreSQL", postgres.IsAvailable, postgres.UnavailableReason);

        if (redis is not null)
        {
            EnsureAvailable("Redis", redis.IsAvailable, redis.UnavailableReason);
        }
    }

    public static void SkipUnlessDockerIsAvailable(RedisFixture redis) =>
        EnsureAvailable("Redis", redis.IsAvailable, redis.UnavailableReason);

    private static void EnsureAvailable(string dependencyName, bool isAvailable, string? unavailableReason)
    {
        if (isAvailable)
        {
            return;
        }

        var message =
            $"{dependencyName} Testcontainer is unavailable. Start Docker to run integration tests."
            + (string.IsNullOrWhiteSpace(unavailableReason) ? string.Empty : $" Reason: {unavailableReason}");

        if (IntegrationRequired)
        {
            throw new InvalidOperationException(message);
        }

        Skip.If(true, message);
    }
}
