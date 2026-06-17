using LooksRatingApi.Contracts.SparksLedgerContracts;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Services.SparksLedger
{
    public sealed class SparksWalletProvisioner : ISparksWalletProvisioner
    {
        private readonly ISparksLedgerRepository _sparksLedgerRepository;
        private readonly ILogger<SparksWalletProvisioner> _logger;

        public SparksWalletProvisioner(
            ISparksLedgerRepository sparksLedgerRepository,
            ILogger<SparksWalletProvisioner> logger)
        {
            _sparksLedgerRepository = sparksLedgerRepository;
            _logger = logger;
        }

        public async Task EnsureForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
            {
                return;
            }

            if (await _sparksLedgerRepository.GetSparksByUserId(userId, cancellationToken) is not null)
            {
                return;
            }

            var walletResult = Models.SparksWallet.Create(userId);
            if (walletResult.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Failed to create sparks wallet for user {userId}: {walletResult.Error}");
            }

            await _sparksLedgerRepository.AddAsync(walletResult.Value, cancellationToken);

            try
            {
                await _sparksLedgerRepository.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Created missing sparks wallet for user {UserId}", userId);
            }
            catch (DbUpdateException ex) when (IsDuplicateWalletException(ex))
            {
                _logger.LogDebug(
                    ex,
                    "Sparks wallet for user {UserId} was created concurrently",
                    userId);

                if (await _sparksLedgerRepository.GetSparksByUserId(userId, cancellationToken) is null)
                {
                    throw new InvalidOperationException(
                        $"Sparks wallet not found for user {userId} after concurrent creation attempt",
                        ex);
                }
            }
        }

        private static bool IsDuplicateWalletException(DbUpdateException exception) =>
            exception.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
            || exception.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true;
    }
}
