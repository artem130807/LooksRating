using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts
{
    public interface IUserReferenceLinkRepository
    {
        Task AddAsync(UserReferenceLink userReferenceLink, CancellationToken cancellationToken = default);

        Task<UserReferenceLink?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<UserReferenceLink> EnsureLinkExistsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<ReferralInviteReservationResult> TryReserveReferralInviteAsync(
            Guid referrerUserId,
            Guid invitedUserId,
            int maxInvited,
            CancellationToken cancellationToken = default);

        Task ReleaseReferralInviteAsync(
            Guid referrerUserId,
            Guid invitedUserId,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}