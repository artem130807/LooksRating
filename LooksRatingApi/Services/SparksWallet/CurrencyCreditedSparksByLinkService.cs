using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;

namespace LooksRatingApi.Services.SparksWallet
{
    public sealed class CurrencyCreditedSparksByLinkService : ICurrencyCreditedSparksByLinkService
    {
        private readonly IUserReferenceLinkRepository _userReferenceLinkRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrencySparksService _currencySparksService;
        private readonly ISparksWalletProvisioner _sparksWalletProvisioner;
        private readonly ILogger<CurrencyCreditedSparksByLinkService> _logger;

        public CurrencyCreditedSparksByLinkService(
            IUserReferenceLinkRepository userReferenceLinkRepository,
            IUserRepository userRepository,
            ICurrencySparksService currencySparksService,
            ISparksWalletProvisioner sparksWalletProvisioner,
            ILogger<CurrencyCreditedSparksByLinkService> logger)
        {
            _userReferenceLinkRepository = userReferenceLinkRepository;
            _userRepository = userRepository;
            _currencySparksService = currencySparksService;
            _sparksWalletProvisioner = sparksWalletProvisioner;
            _logger = logger;
        }

        public async Task CreditReferrerForRegistrationAsync(
            Guid newUserId,
            string? referralLink,
            CancellationToken cancellationToken = default)
        {
            if (!ReferralLinkParser.TryParseReferrerUserId(referralLink, out var referrerUserId))
            {
                return;
            }

            if (referrerUserId == newUserId)
            {
                _logger.LogDebug(
                    "Referral sparks skipped: self-referral for user {UserId}",
                    newUserId);
                return;
            }

            var referrer = await _userRepository.GetUserById(referrerUserId);
            if (referrer is null)
            {
                _logger.LogDebug(
                    "Referral sparks skipped: referrer {ReferrerUserId} not found",
                    referrerUserId);
                return;
            }

            await _userReferenceLinkRepository.EnsureLinkExistsAsync(referrerUserId, cancellationToken);

            var reservation = await _userReferenceLinkRepository.TryReserveReferralInviteAsync(
                referrerUserId,
                newUserId,
                ReferralSparksRules.MaxInvitedUsers,
                cancellationToken);

            switch (reservation.Status)
            {
                case ReferralInviteReservationStatus.LimitReached:
                    _logger.LogInformation(
                        "Referral sparks skipped: referrer {ReferrerUserId} reached invite limit ({Limit})",
                        referrerUserId,
                        ReferralSparksRules.MaxInvitedUsers);
                    return;
                case ReferralInviteReservationStatus.AlreadyInvited:
                    _logger.LogDebug(
                        "Referral sparks skipped: invited user {InvitedUserId} already processed",
                        newUserId);
                    return;
                case ReferralInviteReservationStatus.ReferrerLinkNotFound:
                    _logger.LogWarning(
                        "Referral sparks skipped: reference link missing for referrer {ReferrerUserId}",
                        referrerUserId);
                    return;
            }

            try
            {
                await _sparksWalletProvisioner.EnsureForUserAsync(referrerUserId, cancellationToken);
                await _currencySparksService.Credited(
                    referrerUserId,
                    ReferralSparksRules.ReferralRewardSparks,
                    cancellationToken);

                _logger.LogInformation(
                    "Referral sparks credited: referrer={ReferrerUserId}, newUser={NewUserId}, amount={Amount}, invitedCount={Count}",
                    referrerUserId,
                    newUserId,
                    ReferralSparksRules.ReferralRewardSparks,
                    reservation.InvitedCount);
            }
            catch (Exception ex)
            {
                await _userReferenceLinkRepository.ReleaseReferralInviteAsync(
                    referrerUserId,
                    newUserId,
                    cancellationToken);

                _logger.LogError(
                    ex,
                    "Referral sparks credit failed for referrer {ReferrerUserId}, invited user {InvitedUserId}; reservation released",
                    referrerUserId,
                    newUserId);
            }
        }
    }
}
