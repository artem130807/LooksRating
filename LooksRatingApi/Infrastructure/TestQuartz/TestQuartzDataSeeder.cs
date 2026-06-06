using System.Text.Json;
using System.Text.Json.Serialization;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Domain.Vo;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace LooksRatingApi.Infrastructure.TestQuartz
{
    public sealed class TestQuartzDataSeeder : ITestQuartzDataSeeder
    {
        private const int SeedTelegramIdMin = 910_000;
        private const int SeedTelegramIdMax = 920_000;
        private const int VipProductCode = 1001;

        private readonly LooksRatingDbContext _context;
        private readonly ISeasonRepository _seasonRepository;
        private readonly INormalizeCityNameService _normalizeCityName;
        private readonly IConnectionMultiplexer _redis;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TestQuartzDataSeeder> _logger;

        public TestQuartzDataSeeder(
            LooksRatingDbContext context,
            ISeasonRepository seasonRepository,
            INormalizeCityNameService normalizeCityName,
            IConnectionMultiplexer redis,
            IWebHostEnvironment environment,
            ILogger<TestQuartzDataSeeder> logger)
        {
            _context = context;
            _seasonRepository = seasonRepository;
            _normalizeCityName = normalizeCityName;
            _redis = redis;
            _environment = environment;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("TestQuartz сид: проверка тестовых пользователей");
            var payload = await LoadSeedFileAsync(cancellationToken);
            if (payload is null)
            {
                return;
            }

            await SeedFromFileAsync(payload, cancellationToken);
            await SeedVipPaymentsAsync(payload, cancellationToken);
            await SyncRedisRatingsAsync(cancellationToken);
        }

        private async Task<TestQuartzSeedFile?> LoadSeedFileAsync(CancellationToken cancellationToken)
        {
            var seedFile = Path.Combine(
                _environment.ContentRootPath,
                "Infrastructure",
                "TestQuartz",
                "test-quartz-users.json");

            if (!File.Exists(seedFile))
            {
                _logger.LogWarning("Файл тестовых данных не найден: {Path}", seedFile);
                return null;
            }

            var json = await File.ReadAllTextAsync(seedFile, cancellationToken);
            var payload = JsonSerializer.Deserialize<TestQuartzSeedFile>(json, JsonOptions);
            if (payload?.Categories is null || payload.Categories.Count == 0)
            {
                _logger.LogWarning("Файл тестовых данных пуст или некорректен");
                return null;
            }

            return payload;
        }

        private async Task SeedFromFileAsync(TestQuartzSeedFile payload, CancellationToken cancellationToken)
        {
            var season = await _seasonRepository.GetCurrent();
            if (season is null)
            {
                _logger.LogWarning("Тестовый сид пропущен: нет открытого сезона");
                return;
            }

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductCode == VipProductCode, cancellationToken);
            if (product is null)
            {
                _logger.LogWarning("Тестовый сид пропущен: VIP-продукт не найден");
                return;
            }

            var defaultCity = payload.City.Trim().ToLowerInvariant();
            var createdProfiles = 0;
            var createdUsers = 0;
            var skippedUsers = 0;
            var createdBotPayments = 0;
            var cities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var category in payload.Categories)
            {
                var cityValue = (string.IsNullOrWhiteSpace(category.City) ? defaultCity : category.City)
                    .Trim()
                    .ToLowerInvariant();
                var paymentKind = ParsePaymentKind(category.VipPaymentKind);
                var entries = ExpandCategoryUsers(category);

                foreach (var entry in entries)
                {
                    if (await _context.Users.AnyAsync(u => u.TelegramId == entry.TelegramId, cancellationToken))
                    {
                        skippedUsers++;
                        continue;
                    }

                    var user = new User
                    {
                        Id = Guid.NewGuid(),
                        TelegramId = entry.TelegramId,
                        TelegramUsername = $"test_{entry.TelegramId}",
                        Name = entry.Name,
                        CountInTop = 0,
                        Status = category.Vip || category.VipExpired
                            ? VipStatus.Availlable
                            : VipStatus.Unavaillable,
                    };

                    _context.Users.Add(user);
                    createdUsers++;

                    if (category.Vip || category.VipExpired)
                    {
                        var paidAt = DateTime.UtcNow.AddDays(-entry.VipPaidDaysAgo);
                        if (TryCreateVipPayment(
                                user.Id,
                                product.Id,
                                product.CountStars,
                                entry.TelegramId,
                                paidAt,
                                paymentKind,
                                out var order))
                        {
                            _context.PaymentOrders.Add(order);
                            if (paymentKind == VipPaymentKind.Bot)
                            {
                                createdBotPayments++;
                            }
                        }
                    }

                    var profile = new PhotoProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        SeasonId = season.Id,
                        Rating = entry.Rating,
                        RatingCount = entry.RatingCount,
                        Rank = RankEnum.Average,
                        Status = StatusEnum.Active,
                        CityNomination = CityVo.Create(cityValue).Value,
                        AgeNomination = category.Age,
                        GenderNomination = ParseGender(category.Gender),
                        CreatedAt = DateTime.UtcNow.AddDays(-(entry.TelegramId % 30)),
                    };

                    _context.PhotoProfiles.Add(profile);
                    _context.PhotoProfilePhotos.Add(new PhotoProfilePhoto
                    {
                        Id = Guid.NewGuid(),
                        PhotoProfileId = profile.Id,
                        TelegramFileId = $"test-file-{entry.TelegramId}",
                        SortOrder = 0,
                        CreatedAt = profile.CreatedAt,
                    });

                    createdProfiles++;
                    cities.Add(cityValue);
                }
            }

            if (createdUsers > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Тестовый сид v{SeedVersion}: пользователей +{Users}, профилей +{Profiles}, покупок VIP (бот) +{BotPayments}, пропущено {Skipped}, городов {CityCount} [{Cities}], сезон {SeasonId}",
                payload.SeedVersion,
                createdUsers,
                createdProfiles,
                createdBotPayments,
                skippedUsers,
                cities.Count,
                string.Join(", ", cities.OrderBy(c => c)),
                season.Id);
        }

        private async Task SeedVipPaymentsAsync(TestQuartzSeedFile payload, CancellationToken cancellationToken)
        {
            if (payload.VipPayments is null || payload.VipPayments.Count == 0)
            {
                return;
            }

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductCode == VipProductCode, cancellationToken);
            if (product is null)
            {
                return;
            }

            var created = 0;
            var skipped = 0;

            foreach (var payment in payload.VipPayments)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.TelegramId == payment.TelegramId, cancellationToken);
                if (user is null)
                {
                    _logger.LogWarning("VIP payment seed: пользователь {TelegramId} не найден", payment.TelegramId);
                    continue;
                }

                var chargePrefix = payment.Kind.Equals("bot", StringComparison.OrdinalIgnoreCase)
                    ? "telegram_charge_seed_"
                    : "system:";

                var alreadyHas = await _context.PaymentOrders.AnyAsync(
                    o => o.UserId == user.Id
                        && o.Status == PaymentOrderStatus.Paid
                        && o.TelegramPaymentChargeId != null
                        && o.TelegramPaymentChargeId.StartsWith(chargePrefix),
                    cancellationToken);

                if (alreadyHas && !payment.ReplaceExisting)
                {
                    skipped++;
                    continue;
                }

                if (alreadyHas && payment.ReplaceExisting)
                {
                    var oldOrders = await _context.PaymentOrders
                        .Where(o => o.UserId == user.Id && o.Status == PaymentOrderStatus.Paid)
                        .ToListAsync(cancellationToken);
                    _context.PaymentOrders.RemoveRange(oldOrders);
                }

                var paidAt = DateTime.UtcNow.AddDays(-payment.VipPaidDaysAgo);
                var kind = ParsePaymentKind(payment.Kind);
                if (!TryCreateVipPayment(
                        user.Id,
                        product.Id,
                        product.CountStars,
                        payment.TelegramId,
                        paidAt,
                        kind,
                        out var order))
                {
                    continue;
                }

                if (user.Status != VipStatus.Availlable)
                {
                    user.Status = VipStatus.Availlable;
                }

                _context.PaymentOrders.Add(order);
                created++;
            }

            if (created > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "VIP payment seed: создано {Created} записей покупок/продлений, пропущено {Skipped}",
                created,
                skipped);
        }

        private static List<TestQuartzSeedUser> ExpandCategoryUsers(TestQuartzSeedCategory category)
        {
            if (category.Generate is null)
            {
                return category.Users;
            }

            var g = category.Generate;
            var list = new List<TestQuartzSeedUser>(g.Count);
            for (var i = 0; i < g.Count; i++)
            {
                list.Add(new TestQuartzSeedUser
                {
                    TelegramId = g.TelegramIdStart + i,
                    Name = $"{g.NamePrefix}_{i + 1:D2}",
                    Rating = Math.Max(5.0m, g.RatingFrom + g.RatingStep * i),
                    RatingCount = Math.Max(5, g.RatingCountFrom - i),
                    VipPaidDaysAgo = g.VipPaidDaysAgo,
                });
            }

            return list;
        }

        private static bool TryCreateVipPayment(
            Guid userId,
            Guid productId,
            int amountStars,
            long telegramId,
            DateTime paidAt,
            VipPaymentKind kind,
            out PaymentOrder order)
        {
            order = null!;

            if (kind == VipPaymentKind.Bot)
            {
                var createResult = PaymentOrder.Create(
                    userId,
                    productId,
                    $"vip-buy-{telegramId}",
                    amountStars);

                if (createResult.IsFailure)
                {
                    return false;
                }

                order = createResult.Value;
                order.MarkPaid($"telegram_charge_seed_{telegramId}", $"provider_seed_{telegramId}");
                SetPaidAt(order, paidAt);
                return true;
            }

            var grantResult = PaymentOrder.CreateVipTopExtensionGrant(
                userId,
                productId,
                paidAt,
                $"seed-vip-{telegramId}");

            if (grantResult.IsFailure)
            {
                return false;
            }

            order = grantResult.Value;
            return true;
        }

        private static void SetPaidAt(PaymentOrder order, DateTime paidAt) =>
            typeof(PaymentOrder)
                .GetProperty(nameof(PaymentOrder.PaidAt))!
                .SetValue(order, paidAt);

        private async Task SyncRedisRatingsAsync(CancellationToken cancellationToken)
        {
            var season = await _seasonRepository.GetCurrent();
            if (season is null)
            {
                return;
            }

            var profiles = await _context.PhotoProfiles
                .AsNoTracking()
                .Include(p => p.User)
                .Where(p =>
                    p.SeasonId == season.Id
                    && p.Status == StatusEnum.Active
                    && p.User.TelegramId >= SeedTelegramIdMin
                    && p.User.TelegramId < SeedTelegramIdMax)
                .ToListAsync(cancellationToken);

            if (profiles.Count == 0)
            {
                return;
            }

            var db = _redis.GetDatabase();
            foreach (var profile in profiles)
            {
                var cityKey = _normalizeCityName.Normalize(profile.CityNomination.Value);
                var sortedSetKey = PhotoRedisKeys.RatingSortedSet(cityKey, season.Id);
                var score = PhotoRankingScore.ToSortScore(profile.Rating, profile.RatingCount);
                await db.SortedSetAddAsync(sortedSetKey, profile.Id.ToString(), score);
            }

            _logger.LogInformation(
                "Redis: обновлено {Count} профилей в рейтинговых sorted set для сезона {SeasonId}",
                profiles.Count,
                season.Id);
        }

        private static VipPaymentKind ParsePaymentKind(string? value) =>
            value?.Trim().ToLowerInvariant() switch
            {
                "bot" or "purchase" or "buy" => VipPaymentKind.Bot,
                _ => VipPaymentKind.Grant,
            };

        private static GenderEnum ParseGender(string value) =>
            value.Trim().ToLowerInvariant() switch
            {
                "male" or "мужской" or "m" => GenderEnum.Male,
                "female" or "женский" or "f" => GenderEnum.Female,
                _ => GenderEnum.Unknown,
            };

        private enum VipPaymentKind
        {
            Grant,
            Bot,
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        private sealed class TestQuartzSeedFile
        {
            public int SeedVersion { get; set; }
            public string City { get; set; } = "самара";
            public List<TestQuartzSeedCategory> Categories { get; set; } = [];
            public List<TestQuartzSeedVipPayment> VipPayments { get; set; } = [];
        }

        private sealed class TestQuartzSeedCategory
        {
            public string Label { get; set; } = string.Empty;
            public string? City { get; set; }
            public string Gender { get; set; } = "Male";
            public int Age { get; set; }
            public bool Vip { get; set; }
            public bool VipExpired { get; set; }
            public string? VipPaymentKind { get; set; }
            public TestQuartzSeedGenerate? Generate { get; set; }
            public List<TestQuartzSeedUser> Users { get; set; } = [];
        }

        private sealed class TestQuartzSeedGenerate
        {
            public int Count { get; set; }
            public long TelegramIdStart { get; set; }
            public string NamePrefix { get; set; } = "T";
            public decimal RatingFrom { get; set; } = 9.0m;
            public decimal RatingStep { get; set; } = -0.1m;
            public int RatingCountFrom { get; set; } = 40;
            public int VipPaidDaysAgo { get; set; } = 10;
        }

        private sealed class TestQuartzSeedUser
        {
            public long TelegramId { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Rating { get; set; }
            public int RatingCount { get; set; }
            public int VipPaidDaysAgo { get; set; } = 10;
        }

        private sealed class TestQuartzSeedVipPayment
        {
            public long TelegramId { get; set; }
            public string Kind { get; set; } = "bot";
            public int VipPaidDaysAgo { get; set; } = 10;
            public bool ReplaceExisting { get; set; }
        }
    }
}
