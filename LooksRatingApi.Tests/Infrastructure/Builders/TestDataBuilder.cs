using LooksRatingApi;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Tests.Infrastructure.Builders;

internal static class TestDataBuilder
{
    public static async Task<(ListSeasons Chapter, Season Season)> SeedOpenSeasonAsync(
        LooksRatingDbContext context,
        int seasonNumber = 1,
        string seasonName = "Test season",
        CancellationToken cancellationToken = default)
    {
        var chapter = ListSeasons.Create().Value;
        var season = Season.Create(seasonName, seasonNumber, chapter.Id).Value;

        context.ListSeasons.Add(chapter);
        context.Seasons.Add(season);
        await context.SaveChangesAsync(cancellationToken);

        return (chapter, season);
    }

    public static async Task<User> SeedUserAsync(
        LooksRatingDbContext context,
        long telegramId,
        VipStatus vipStatus = VipStatus.Unavaillable,
        CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = telegramId,
            TelegramUsername = $"user_{telegramId}",
            Name = $"User {telegramId}",
            Status = vipStatus,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public static async Task<UserSession> SeedSessionAsync(
        LooksRatingDbContext context,
        long telegramId,
        BotSessionState state = BotSessionState.Start,
        CancellationToken cancellationToken = default)
    {
        var session = UserSession.Create(telegramId, state).Value;
        context.UserSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);
        return session;
    }

    public static async Task<PhotoProfile> SeedPhotoProfileAsync(
        LooksRatingDbContext context,
        User user,
        Season season,
        StatusEnum status = StatusEnum.Active,
        int photoCount = 1,
        CancellationToken cancellationToken = default)
    {
        var city = CityVo.Create("moscow").Value;
        var profile = new PhotoProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SeasonId = season.Id,
            Rating = 7.5m,
            RatingCount = 10,
            Rank = RankEnum.Cute,
            Status = status,
            CityNomination = city,
            AgeNomination = 25,
            GenderNomination = GenderEnum.Male,
            CreatedAt = DateTime.UtcNow,
        };

        for (var i = 0; i < photoCount; i++)
        {
            profile.Photos.Add(new PhotoProfilePhoto
            {
                Id = Guid.NewGuid(),
                PhotoProfileId = profile.Id,
                TelegramFileId = $"file-{profile.Id:N}-{i}",
                SortOrder = i,
                CreatedAt = DateTime.UtcNow,
            });
        }

        context.PhotoProfiles.Add(profile);
        await context.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public static async Task<Product> SeedVipProductAsync(
        LooksRatingDbContext context,
        CancellationToken cancellationToken = default)
    {
        var product = Product.Create("VIP", VipTopRules.VipProductCode, VipTopRules.VipStarsPrice, "XTR", VipTopRules.DefaultVipDays).Value;
        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);
        return product;
    }

    public static async Task<PaymentOrder> SeedPaidVipOrderAsync(
        LooksRatingDbContext context,
        User user,
        Product product,
        DateTime paidAtUtc,
        CancellationToken cancellationToken = default)
    {
        var order = PaymentOrder.Create(user.Id, product.Id, $"vip-{user.TelegramId}", 100).Value;
        order.MarkPaid($"charge-{user.TelegramId}", "provider");

        context.PaymentOrders.Add(order);
        await context.SaveChangesAsync(cancellationToken);

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE "PaymentOrder"
             SET "PaidAt" = {paidAtUtc}
             WHERE "Id" = {order.Id}
             """,
            cancellationToken);

        return order;
    }
}
