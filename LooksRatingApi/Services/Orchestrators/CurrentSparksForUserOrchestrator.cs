using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PaymentOrderContracts;
using LooksRatingApi.Contracts.ProductContracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Infrastructure.DistributedLock;
using LooksRatingApi.Models;
using LooksRatingApi.Services.SparksWallet;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Services.Orchestrators
{
    public sealed class CurrentSparksForUserOrchestrator : ICurrentSparksForUserOrchestrator
    {
        private static readonly TimeSpan LockTtl = TimeSpan.FromMinutes(2);

        private readonly IUserRepository _userRepository;
        private readonly ICurrencySparksService _currencySparksService;
        private readonly ISparksWalletProvisioner _sparksWalletProvisioner;
        private readonly IPaymentOrderRepository _paymentOrderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IRedisDistributedLock _distributedLock;
        private readonly LooksRatingDbContext _context;
        private readonly ILogger<CurrentSparksForUserOrchestrator> _logger;

        public CurrentSparksForUserOrchestrator(
            IUserRepository userRepository,
            ICurrencySparksService currencySparksService,
            ISparksWalletProvisioner sparksWalletProvisioner,
            IPaymentOrderRepository paymentOrderRepository,
            IProductRepository productRepository,
            IRedisDistributedLock distributedLock,
            LooksRatingDbContext context,
            ILogger<CurrentSparksForUserOrchestrator> logger)
        {
            _userRepository = userRepository;
            _currencySparksService = currencySparksService;
            _sparksWalletProvisioner = sparksWalletProvisioner;
            _paymentOrderRepository = paymentOrderRepository;
            _productRepository = productRepository;
            _distributedLock = distributedLock;
            _context = context;
            _logger = logger;
        }

        public async Task<ChannelSubscribeBonusResult> ProcessAsync(
            long telegramId,
            bool credit,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetUserByTelegramId(telegramId);
            if (user is null)
            {
                return new ChannelSubscribeBonusResult(
                    false,
                    "Пользователь не найден",
                    ChannelSubscribeBonusStatus.UserNotFound);
            }

            if (user.IssubscribeChannel)
            {
                return new ChannelSubscribeBonusResult(
                    true,
                    "Отлично, вы уже были подписаны",
                    ChannelSubscribeBonusStatus.AlreadyCredited);
            }

            if (!credit)
            {
                return new ChannelSubscribeBonusResult(
                    true,
                    "Бонус за подписку доступен",
                    ChannelSubscribeBonusStatus.Eligible);
            }

            await using var lockHandle = await _distributedLock.TryAcquireAsync(
                DistributedLockKeys.ChannelSubscribeBonus(telegramId),
                LockTtl,
                cancellationToken);

            if (lockHandle is null)
            {
                _logger.LogInformation(
                    "Channel subscribe bonus skipped: lock is held for telegramId={TelegramId}",
                    telegramId);
                return new ChannelSubscribeBonusResult(
                    false,
                    "Не удалось начислить бонус. Попробуйте позже",
                    ChannelSubscribeBonusStatus.Failed);
            }

            user = await _userRepository.GetUserByTelegramId(telegramId);
            if (user is null)
            {
                return new ChannelSubscribeBonusResult(
                    false,
                    "Пользователь не найден",
                    ChannelSubscribeBonusStatus.UserNotFound);
            }

            if (user.IssubscribeChannel)
            {
                return new ChannelSubscribeBonusResult(
                    true,
                    "Отлично, вы уже были подписаны",
                    ChannelSubscribeBonusStatus.AlreadyCredited);
            }

            var payload = ChannelSubscribeSparksRules.BuildPayload(telegramId);
            var existingPayloads = await _paymentOrderRepository.GetExistingPaidPayloadsAsync(
                [payload],
                cancellationToken);

            if (existingPayloads.Contains(payload))
            {
                await MarkSubscribedAsync(user.Id, cancellationToken);
                return new ChannelSubscribeBonusResult(
                    true,
                    "Отлично, вы уже были подписаны",
                    ChannelSubscribeBonusStatus.AlreadyCredited);
            }

            var product = await _productRepository.GetByCodeAsync(
                ChannelSubscribeSparksRules.RewardProductCode,
                cancellationToken);

            if (product is null)
            {
                _logger.LogError(
                    "Channel subscribe bonus failed: product {ProductCode} not found",
                    ChannelSubscribeSparksRules.RewardProductCode);
                return new ChannelSubscribeBonusResult(
                    false,
                    "Не удалось начислить бонус. Попробуйте позже",
                    ChannelSubscribeBonusStatus.Failed);
            }

            var grant = PaymentOrder.CreateSparksRewardGrant(user.Id, product.Id, payload);
            if (grant.IsFailure)
            {
                _logger.LogWarning(
                    "Channel subscribe grant skipped for telegramId={TelegramId}: {Error}",
                    telegramId,
                    grant.Error);
                return new ChannelSubscribeBonusResult(
                    false,
                    "Не удалось начислить бонус. Попробуйте позже",
                    ChannelSubscribeBonusStatus.Failed);
            }

            if (!await TryPersistGrantAsync(grant.Value, cancellationToken))
            {
                await MarkSubscribedAsync(user.Id, cancellationToken);
                return new ChannelSubscribeBonusResult(
                    true,
                    "Отлично, вы уже были подписаны",
                    ChannelSubscribeBonusStatus.AlreadyCredited);
            }

            try
            {
                await _sparksWalletProvisioner.EnsureForUserAsync(user.Id, cancellationToken);
                await _currencySparksService.Credited(
                    user.Id,
                    ChannelSubscribeSparksRules.RewardSparks,
                    cancellationToken);
                await MarkSubscribedAsync(user.Id, cancellationToken);

                _logger.LogInformation(
                    "Channel subscribe bonus credited: user={UserId}, telegramId={TelegramId}, amount={Amount}",
                    user.Id,
                    telegramId,
                    ChannelSubscribeSparksRules.RewardSparks);

                return new ChannelSubscribeBonusResult(
                    true,
                    $"Начислено {ChannelSubscribeSparksRules.RewardSparks:0} искр за подписку на сообщество",
                    ChannelSubscribeBonusStatus.Credited);
            }
            catch (Exception ex)
            {
                await RemoveGrantByPayloadAsync(payload, cancellationToken);
                _logger.LogError(
                    ex,
                    "Channel subscribe bonus failed for telegramId={TelegramId}",
                    telegramId);

                return new ChannelSubscribeBonusResult(
                    false,
                    "Не удалось начислить бонус. Попробуйте позже",
                    ChannelSubscribeBonusStatus.Failed);
            }
        }

        private async Task MarkSubscribedAsync(Guid userId, CancellationToken cancellationToken)
        {
            var user = await _context.Users.FindAsync([userId], cancellationToken);
            if (user is null || user.IssubscribeChannel)
            {
                return;
            }

            user.IssubscribeChannel = true;
            await _context.SaveChangesAsync(cancellationToken);
        }

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
                    "Channel subscribe grant already exists for payload {Payload}",
                    grant.Payload);
                return false;
            }
        }

        private async Task RemoveGrantByPayloadAsync(string payload, CancellationToken cancellationToken)
        {
            var grants = await _context.PaymentOrders
                .Where(order => order.Payload == payload)
                .ToListAsync(cancellationToken);

            if (grants.Count == 0)
            {
                return;
            }

            _context.PaymentOrders.RemoveRange(grants);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
