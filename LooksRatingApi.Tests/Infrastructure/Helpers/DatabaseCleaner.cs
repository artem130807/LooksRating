using LooksRatingApi;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Tests.Infrastructure.Helpers;

internal static class DatabaseCleaner
{
    public static async Task ResetAsync(LooksRatingDbContext context, CancellationToken cancellationToken = default)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                "ReferralInvite",
                "UserReferenceLink",
                "PhotoProfilePhoto",
                "PhotoProfile",
                "PhotoUser",
                "Review",
                "UserTicket",
                "TheBestWeek",
                "PaymentOrder",
                "SparksLedger",
                "EventStores",
                "UserSession",
                "RecomendationSettings",
                "Season",
                "ListSeasons",
                "Product",
                "User"
            RESTART IDENTITY CASCADE;
            """,
            cancellationToken);
    }
}
