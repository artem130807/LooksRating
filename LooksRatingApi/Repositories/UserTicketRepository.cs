using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.UserTicketContracts;
using LooksRatingApi.Infrastructure.DistributedLock;
using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class UserTicketRepository : IUserTicketRepository
    {
        private static readonly TimeSpan PhotoProfileLockTtl = TimeSpan.FromMinutes(5);

        private readonly LooksRatingDbContext _context;
        private readonly IRedisDistributedLock _distributedLock;

        public UserTicketRepository(
            LooksRatingDbContext context,
            IRedisDistributedLock distributedLock)
        {
            _context = context;
            _distributedLock = distributedLock;
        }

        public async Task Create(UserTicket ticket)
        {
            _context.UserTickets.Add(ticket);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(Guid Id)
        {
            await _context.UserTickets.Where(x => x.Id == Id).ExecuteDeleteAsync();
        }

        public async Task<UserTicket?> GetTicketById(Guid Id)
        {
            return await _context.UserTickets
                .Include(x => x.User)
                    .ThenInclude(u => u.RecomendationSettings)
                .Include(x => x.PhotoProfile)
                    .ThenInclude(p => p.User)
                .Include(x => x.PhotoProfile)
                    .ThenInclude(p => p.Photos.OrderBy(x => x.SortOrder))
                .FirstOrDefaultAsync(x => x.Id == Id);
        }

        public async Task<UserTicket?> GetTicketByTelegramId(long telegramId)
        {
            return await _context.UserTickets.Include(x => x.User).FirstOrDefaultAsync(x => x.User.TelegramId == telegramId);
        }

        public async Task<List<UserTicket>> GetTicketsByUsersCity(string city)
        {
            return await _context.UserTickets
                .Include(x => x.User)
                    .ThenInclude(u => u.RecomendationSettings)
                .Include(x => x.PhotoProfile)
                    .ThenInclude(p => p.Photos.OrderBy(x => x.SortOrder))
                .Where(x => x.User.RecomendationSettings != null
                    && x.User.RecomendationSettings.City.Value == city)
                .OrderByDescending(x => x.OccuredAt)
                .ToListAsync();
        }

        public async Task<List<UserTicket>> GetTicketsByProfileCity(string city, int skip, int take)
        {
            if (take <= 0)
            {
                take = 50;
            }

            return await _context.UserTickets
                .AsNoTracking()
                .Include(x => x.User)
                    .ThenInclude(u => u.RecomendationSettings)
                .Include(x => x.PhotoProfile)
                    .ThenInclude(p => p.User)
                .Include(x => x.PhotoProfile)
                    .ThenInclude(p => p.Photos.OrderBy(photo => photo.SortOrder))
                .Where(x => x.PhotoProfile.CityNomination.Value == city)
                .OrderByDescending(x => x.OccuredAt)
                .Skip(Math.Max(skip, 0))
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> CountTicketsByProfileCity(string city)
        {
            return await _context.UserTickets
                .AsNoTracking()
                .Where(x => x.PhotoProfile.CityNomination.Value == city)
                .CountAsync();
        }

        public async Task<Guid?> GetTicketIdAtOffsetByProfileCity(string city, int offset)
        {
            if (offset < 0)
            {
                offset = 0;
            }

            var ticketId = await _context.UserTickets
                .AsNoTracking()
                .Where(x => x.PhotoProfile.CityNomination.Value == city)
                .OrderByDescending(x => x.OccuredAt)
                .Skip(offset)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            return ticketId == Guid.Empty ? null : ticketId;
        }

        public async Task<List<string>> GetCitiesWithPendingTickets()
        {
            if (!await _context.UserTickets.AsNoTracking().AnyAsync())
            {
                return [];
            }

            var profileIds = await _context.UserTickets
                .AsNoTracking()
                .Select(ticket => ticket.PhotoProfileId)
                .Distinct()
                .ToListAsync();

            return await _context.PhotoProfiles
                .AsNoTracking()
                .Where(profile => profileIds.Contains(profile.Id))
                .Select(profile => profile.CityNomination.Value ?? string.Empty)
                .Where(city => city != string.Empty)
                .Distinct()
                .OrderBy(city => city)
                .ToListAsync();
        }

        public async Task<bool> ExistsByReporterAndProfile(Guid reporterUserId, Guid photoProfileId)
        {
            return await _context.UserTickets
                .AnyAsync(x => x.UserId == reporterUserId && x.PhotoProfileId == photoProfileId);
        }

        public async Task<HashSet<Guid>> GetReportedPhotoProfileIdsByReporterAsync(
            Guid reporterUserId,
            CancellationToken cancellationToken = default)
        {
            var profileIds = await _context.UserTickets
                .AsNoTracking()
                .Where(ticket => ticket.UserId == reporterUserId)
                .Select(ticket => ticket.PhotoProfileId)
                .Distinct()
                .ToListAsync(cancellationToken);

            return profileIds.ToHashSet();
        }

        public async Task Update(UserTicket ticket)
        {
            _context.UserTickets.Update(ticket);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveUserTickets(Guid photoProfileId)
        {
            await _context.UserTickets
            .Where(x => x.PhotoProfileId == photoProfileId)
            .ExecuteDeleteAsync();
        }

        public async Task<string?> LockPhotoProfileAsync(
            Guid photoProfileId,
            CancellationToken cancellationToken = default)
        {
            if (photoProfileId == Guid.Empty)
                throw new ArgumentException("Photo profile id is required.", nameof(photoProfileId));

            var handle = await _distributedLock.TryAcquireAsync(
                DistributedLockKeys.UserTicketPhotoProfile(photoProfileId),
                PhotoProfileLockTtl,
                cancellationToken);

            return handle?.Token;
        }

        public Task<bool> UnlockPhotoProfileAsync(
            Guid photoProfileId,
            string lockToken,
            CancellationToken cancellationToken = default)
        {
            if (photoProfileId == Guid.Empty)
                throw new ArgumentException("Photo profile id is required.", nameof(photoProfileId));
            if (string.IsNullOrWhiteSpace(lockToken))
                throw new ArgumentException("Lock token is required.", nameof(lockToken));

            return _distributedLock.ReleaseAsync(
                DistributedLockKeys.UserTicketPhotoProfile(photoProfileId),
                lockToken,
                cancellationToken);
        }

        public Task<bool> IsPhotoProfileLockedAsync(
            Guid photoProfileId,
            CancellationToken cancellationToken = default)
        {
            if (photoProfileId == Guid.Empty)
                throw new ArgumentException("Photo profile id is required.", nameof(photoProfileId));

            return _distributedLock.IsLockedAsync(
                DistributedLockKeys.UserTicketPhotoProfile(photoProfileId),
                cancellationToken);
        }

        public async Task<List<UserTicket>> GetUserTicketsByPhotoProfileId(Guid photoProfileId)
        {
           return await _context.UserTickets.Where(x => x.PhotoProfileId == photoProfileId).ToListAsync();
        }
    }
}