using LooksRatingApi.Contracts;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LooksRatingApi.Repositories
{
    public sealed class UserReferenceLinkRepository : IUserReferenceLinkRepository
    {
        private readonly LooksRatingDbContext _context;

        public UserReferenceLinkRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(
            UserReferenceLink userReferenceLink,
            CancellationToken cancellationToken = default)
        {
            await _context.UserReferenceLinks.AddAsync(userReferenceLink, cancellationToken);
        }

        public Task<UserReferenceLink?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return _context.UserReferenceLinks
                .AsNoTracking()
                .FirstOrDefaultAsync(link => link.UserId == userId, cancellationToken);
        }

        public async Task<UserReferenceLink> EnsureLinkExistsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var existing = await _context.UserReferenceLinks
                .AsNoTracking()
                .FirstOrDefaultAsync(link => link.UserId == userId, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }

            var createResult = UserReferenceLink.Create(userId);
            if (createResult.IsFailure)
            {
                throw new InvalidOperationException(createResult.Error);
            }

            await _context.UserReferenceLinks.AddAsync(createResult.Value, cancellationToken);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                return createResult.Value;
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _context.ChangeTracker.Clear();
                return await _context.UserReferenceLinks
                    .AsNoTracking()
                    .FirstAsync(link => link.UserId == userId, cancellationToken);
            }
        }

        public async Task<ReferralInviteReservationResult> TryReserveReferralInviteAsync(
            Guid referrerUserId,
            Guid invitedUserId,
            int maxInvited,
            CancellationToken cancellationToken = default)
        {
            if (await _context.ReferralInvites
                    .AsNoTracking()
                    .AnyAsync(invite => invite.InvitedUserId == invitedUserId, cancellationToken))
            {
                return new ReferralInviteReservationResult(ReferralInviteReservationStatus.AlreadyInvited);
            }

            var linkExists = await _context.UserReferenceLinks
                .AsNoTracking()
                .AnyAsync(link => link.UserId == referrerUserId, cancellationToken);
            if (!linkExists)
            {
                return new ReferralInviteReservationResult(
                    ReferralInviteReservationStatus.ReferrerLinkNotFound);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.ReferralInvites.AddAsync(
                    ReferralInvite.Create(referrerUserId, invitedUserId),
                    cancellationToken);

                var rowsUpdated = await TryIncrementInviteCountAsync(
                    referrerUserId,
                    maxInvited,
                    cancellationToken);

                if (rowsUpdated == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _context.ChangeTracker.Clear();
                    return new ReferralInviteReservationResult(ReferralInviteReservationStatus.LimitReached);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                _context.ChangeTracker.Clear();

                var invitedCount = await _context.UserReferenceLinks
                    .AsNoTracking()
                    .Where(item => item.UserId == referrerUserId)
                    .Select(item => item.CountInvited)
                    .SingleAsync(cancellationToken);

                return new ReferralInviteReservationResult(
                    ReferralInviteReservationStatus.Reserved,
                    invitedCount);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                await transaction.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                return new ReferralInviteReservationResult(ReferralInviteReservationStatus.AlreadyInvited);
            }
        }

        private async Task<int> TryIncrementInviteCountAsync(
            Guid referrerUserId,
            int maxInvited,
            CancellationToken cancellationToken)
        {
            if (_context.Database.IsRelational())
            {
                return await _context.UserReferenceLinks
                    .Where(item =>
                        item.UserId == referrerUserId
                        && item.CountInvited < maxInvited)
                    .ExecuteUpdateAsync(
                        setter => setter.SetProperty(
                            item => item.CountInvited,
                            item => item.CountInvited + 1),
                        cancellationToken);
            }

            var link = await _context.UserReferenceLinks
                .FirstOrDefaultAsync(item => item.UserId == referrerUserId, cancellationToken);
            if (link is null || link.CountInvited >= maxInvited)
            {
                return 0;
            }

            link.AddCountInvited();
            return 1;
        }

        private async Task TryDecrementInviteCountAsync(
            Guid referrerUserId,
            CancellationToken cancellationToken)
        {
            if (_context.Database.IsRelational())
            {
                await _context.UserReferenceLinks
                    .Where(link => link.UserId == referrerUserId && link.CountInvited > 0)
                    .ExecuteUpdateAsync(
                        setter => setter.SetProperty(
                            link => link.CountInvited,
                            link => link.CountInvited - 1),
                        cancellationToken);
                return;
            }

            var link = await _context.UserReferenceLinks
                .FirstOrDefaultAsync(item => item.UserId == referrerUserId, cancellationToken);
            if (link is null)
            {
                return;
            }

            link.RemoveCountInvited();
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task ReleaseReferralInviteAsync(
            Guid referrerUserId,
            Guid invitedUserId,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var deleted = await DeleteReferralInviteAsync(
                    referrerUserId,
                    invitedUserId,
                    cancellationToken);

                if (deleted > 0)
                {
                    await TryDecrementInviteCountAsync(referrerUserId, cancellationToken);
                    if (!_context.Database.IsRelational())
                    {
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                _context.ChangeTracker.Clear();
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        private async Task<int> DeleteReferralInviteAsync(
            Guid referrerUserId,
            Guid invitedUserId,
            CancellationToken cancellationToken)
        {
            if (_context.Database.IsRelational())
            {
                return await _context.ReferralInvites
                    .Where(invite =>
                        invite.ReferrerUserId == referrerUserId
                        && invite.InvitedUserId == invitedUserId)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            var invite = await _context.ReferralInvites
                .FirstOrDefaultAsync(
                    item =>
                        item.ReferrerUserId == referrerUserId
                        && item.InvitedUserId == invitedUserId,
                    cancellationToken);
            if (invite is null)
            {
                return 0;
            }

            _context.ReferralInvites.Remove(invite);
            return 1;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _context.SaveChangesAsync(cancellationToken);

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            for (var current = exception.InnerException; current is not null; current = current.InnerException)
            {
                if (current is PostgresException postgres
                    && postgres.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    return true;
                }

                if (current.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                    || current.Message.Contains("unique", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
