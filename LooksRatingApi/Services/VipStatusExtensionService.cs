using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PaymentOrderContracts;
using LooksRatingApi.Contracts.ProductContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Services
{
    public sealed class VipStatusExtensionService : IVipStatusExtensionService
    {
        private readonly LooksRatingDbContext _context;
        private readonly IProductRepository _productRepository;
        private readonly IPaymentOrderRepository _paymentOrderRepository;
        private readonly IVipExpirationReadService _vipExpirationReadService;
        private readonly ILogger<VipStatusExtensionService> _logger;

        public VipStatusExtensionService(
            LooksRatingDbContext context,
            IProductRepository productRepository,
            IPaymentOrderRepository paymentOrderRepository,
            IVipExpirationReadService vipExpirationReadService,
            ILogger<VipStatusExtensionService> logger)
        {
            _context = context;
            _productRepository = productRepository;
            _paymentOrderRepository = paymentOrderRepository;
            _vipExpirationReadService = vipExpirationReadService;
            _logger = logger;
        }

        public Task<VipStatusExtensionResult> ExtendByTelegramIdsAsync(
            IReadOnlyCollection<long> telegramIds,
            Guid seasonId,
            CancellationToken cancellationToken = default) =>
            ExtendByTelegramIdsCoreAsync(telegramIds, seasonId, allowRetry: true, cancellationToken);

        private async Task<VipStatusExtensionResult> ExtendByTelegramIdsCoreAsync(
            IReadOnlyCollection<long> telegramIds,
            Guid seasonId,
            bool allowRetry,
            CancellationToken cancellationToken)
        {
            var distinctTelegramIds = telegramIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (distinctTelegramIds.Count == 0)
            {
                return new VipStatusExtensionResult(0, 0, 0);
            }

            var product = await _productRepository.GetByCodeAsync(VipTopRules.VipProductCode, cancellationToken);
            if (product is null)
            {
                _logger.LogError(
                    "VIP-продукт {ProductCode} не найден, продление для {Count} пользователей пропущено",
                    VipTopRules.VipProductCode,
                    distinctTelegramIds.Count);
                return new VipStatusExtensionResult(0, 0, distinctTelegramIds.Count);
            }

            var users = await _context.Users
                .Where(user => distinctTelegramIds.Contains(user.TelegramId))
                .ToListAsync(cancellationToken);

            var notFound = distinctTelegramIds.Count - users.Count;
            if (users.Count == 0)
            {
                return new VipStatusExtensionResult(0, 0, notFound);
            }

            var utcNow = DateTime.UtcNow;
            var periodKey = VipTopRewardPeriod.BuildKey(seasonId, utcNow);
            var payloads = users
                .Select(user => VipTopRewardPeriod.BuildExtensionPayload(periodKey, user.TelegramId))
                .ToList();

            var existingPayloads = await _paymentOrderRepository.GetExistingPaidPayloadsAsync(
                payloads,
                cancellationToken);

            var userIds = users.Select(user => user.Id).ToList();
            var expirations = await _vipExpirationReadService.GetExpirationUtcByUserIdsAsync(
                userIds,
                cancellationToken);

            var orders = new List<PaymentOrder>();
            var usersToActivate = new List<Guid>();
            var extended = 0;
            var skipped = 0;

            foreach (var user in users)
            {
                var payload = VipTopRewardPeriod.BuildExtensionPayload(periodKey, user.TelegramId);
                if (existingPayloads.Contains(payload))
                {
                    skipped++;
                    continue;
                }

                var anchor = expirations.TryGetValue(user.Id, out var expiresAt) && expiresAt > utcNow
                    ? expiresAt
                    : utcNow;

                var grant = PaymentOrder.CreateVipTopExtensionGrant(
                    user.Id,
                    product.Id,
                    anchor,
                    payload);

                if (grant.IsFailure)
                {
                    _logger.LogWarning(
                        "VIP grant skipped for telegram {TelegramId}: {Error}",
                        user.TelegramId,
                        grant.Error);
                    skipped++;
                    continue;
                }

                orders.Add(grant.Value);
                usersToActivate.Add(user.Id);
                extended++;
            }

            if (orders.Count == 0)
            {
                return new VipStatusExtensionResult(0, skipped, notFound);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.PaymentOrders.AddRangeAsync(orders, cancellationToken);

                await _context.Users
                    .Where(user => usersToActivate.Contains(user.Id) && user.Status != VipStatus.Availlable)
                    .ExecuteUpdateAsync(
                        setter => setter.SetProperty(u => u.Status, VipStatus.Availlable),
                        cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                if (allowRetry)
                {
                    _logger.LogWarning(
                        ex,
                        "Конфликт при пакетном продлении VIP (период {PeriodKey}), повторная попытка",
                        periodKey);

                    _context.ChangeTracker.Clear();

                    return await ExtendByTelegramIdsCoreAsync(
                        distinctTelegramIds,
                        seasonId,
                        allowRetry: false,
                        cancellationToken);
                }

                _logger.LogError(
                    ex,
                    "Повторное продление VIP не удалось (период {PeriodKey})",
                    periodKey);

                return new VipStatusExtensionResult(0, skipped + extended, notFound);
            }

            if (extended > 0)
            {
                _logger.LogInformation(
                    "VIP продлён для {Extended} пользователей (период {PeriodKey}), пропущено {Skipped}, не найдено {NotFound}",
                    extended,
                    periodKey,
                    skipped,
                    notFound);
            }

            return new VipStatusExtensionResult(extended, skipped, notFound);
        }
    }
}
