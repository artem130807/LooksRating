using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PaymentOrderContracts;
using LooksRatingApi.Contracts.ProductContracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Services.SparksWallet
{
    public sealed class SparksRewardCreditingService : ISparksRewardCreditingService
    {
        private readonly ICurrencySparksService _currencySparksService;
        private readonly IPaymentOrderRepository _paymentOrderRepository;
        private readonly IProductRepository _productRepository;
        private readonly LooksRatingDbContext _context;
        private readonly ILogger<SparksRewardCreditingService> _logger;

        public SparksRewardCreditingService(
            ICurrencySparksService currencySparksService,
            IPaymentOrderRepository paymentOrderRepository,
            IProductRepository productRepository,
            LooksRatingDbContext context,
            ILogger<SparksRewardCreditingService> logger)
        {
            _currencySparksService = currencySparksService;
            _paymentOrderRepository = paymentOrderRepository;
            _productRepository = productRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<SparksRewardCreditingResult> CreditAsync(
            IReadOnlyList<SparksRewardRecipient> recipients,
            int productCode,
            string rewardSource,
            CancellationToken cancellationToken = default)
        {
            if (recipients.Count == 0)
            {
                return new SparksRewardCreditingResult(0, 0, 0, 0);
            }

            var product = await _productRepository.GetByCodeAsync(productCode, cancellationToken);
            if (product is null)
            {
                _logger.LogError(
                    "Product {ProductCode} not found, {RewardSource} sparks rewards for {Count} recipients failed",
                    productCode,
                    rewardSource,
                    recipients.Count);
                return new SparksRewardCreditingResult(0, 0, 0, recipients.Count);
            }

            var payloads = recipients
                .Select(recipient => recipient.Payload)
                .ToList();

            var existingPayloads = await _paymentOrderRepository.GetExistingPaidPayloadsAsync(
                payloads,
                cancellationToken);

            var telegramIds = recipients
                .Select(recipient => recipient.TelegramId)
                .Distinct()
                .ToList();

            var users = await _context.Users
                .AsNoTracking()
                .Where(user => telegramIds.Contains(user.TelegramId))
                .ToListAsync(cancellationToken);

            var usersByTelegramId = users.ToDictionary(user => user.TelegramId);

            var walletUserIds = users.Select(user => user.Id).ToList();
            var wallets = await _context.SparksLedgers
                .AsNoTracking()
                .Where(wallet => walletUserIds.Contains(wallet.UserId))
                .Select(wallet => wallet.UserId)
                .ToListAsync(cancellationToken);
            var walletUserIdSet = wallets.ToHashSet();

            var credited = 0;
            var skipped = 0;
            var notFound = 0;
            var failed = 0;
            var pendingCredits = new List<PendingSparksCredit>();

            foreach (var recipient in recipients)
            {
                if (existingPayloads.Contains(recipient.Payload))
                {
                    skipped++;
                    continue;
                }

                if (!usersByTelegramId.TryGetValue(recipient.TelegramId, out var user))
                {
                    notFound++;
                    continue;
                }

                if (!walletUserIdSet.Contains(user.Id))
                {
                    _logger.LogWarning(
                        "{RewardSource} sparks wallet missing for telegram {TelegramId}, place {Place}",
                        rewardSource,
                        recipient.TelegramId,
                        recipient.Place);
                    notFound++;
                    continue;
                }

                var grant = PaymentOrder.CreateSparksRewardGrant(user.Id, product.Id, recipient.Payload);
                if (grant.IsFailure)
                {
                    _logger.LogWarning(
                        "{RewardSource} sparks grant skipped for telegram {TelegramId}: {Error}",
                        rewardSource,
                        recipient.TelegramId,
                        grant.Error);
                    skipped++;
                    continue;
                }

                var grantPersisted = await TryPersistGrantAsync(grant.Value, cancellationToken);
                if (!grantPersisted)
                {
                    existingPayloads.Add(recipient.Payload);
                    skipped++;
                    continue;
                }

                existingPayloads.Add(recipient.Payload);
                pendingCredits.Add(new PendingSparksCredit(
                    user.Id,
                    recipient.SparksAmount,
                    recipient.TelegramId,
                    recipient.Place,
                    recipient.Payload));
            }

            foreach (var userCredits in pendingCredits.GroupBy(credit => credit.UserId))
            {
                var totalAmount = userCredits.Sum(credit => credit.Amount);
                var credits = userCredits.ToList();
                var first = credits[0];

                try
                {
                    await _currencySparksService.Credited(userCredits.Key, totalAmount, cancellationToken);
                    credited += credits.Count;
                    foreach (var credit in credits)
                    {
                        _logger.LogInformation(
                            "{RewardSource} sparks credited: telegram={TelegramId}, place={Place}, amount={Amount}, payload={Payload}",
                            rewardSource,
                            credit.TelegramId,
                            credit.Place,
                            credit.Amount,
                            credit.Payload);
                    }
                }
                catch (Exception ex)
                {
                    failed += credits.Count;
                    foreach (var credit in credits)
                    {
                        await RemoveGrantByPayloadAsync(credit.Payload, cancellationToken);
                    }

                    _logger.LogError(
                        ex,
                        "{RewardSource}: failed to credit {TotalAmount} sparks for telegram {TelegramId} ({GrantCount} grants); markers removed for retry",
                        rewardSource,
                        totalAmount,
                        first.TelegramId,
                        credits.Count);
                }
            }

            return new SparksRewardCreditingResult(credited, skipped, notFound, failed);
        }

        private sealed record PendingSparksCredit(
            Guid UserId,
            decimal Amount,
            long TelegramId,
            int Place,
            string Payload);

        private async Task<bool> TryPersistGrantAsync(PaymentOrder grant, CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.PaymentOrders.AddAsync(grant, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                _logger.LogWarning(
                    ex,
                    "Sparks grant marker already exists or conflict for payload {Payload}",
                    grant.Payload);
                return false;
            }
        }

        private async Task RemoveGrantByPayloadAsync(string payload, CancellationToken cancellationToken)
        {
            await _context.PaymentOrders
                .Where(order => order.Payload == payload)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
