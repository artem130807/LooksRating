using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.UserTicketContracts
{
    public interface IUserTicketRepository
    {
        Task Create(UserTicket ticket);
        Task Delete(Guid Id);
        Task Update(UserTicket ticket);
        Task<UserTicket?> GetTicketById(Guid Id);
        Task<UserTicket?> GetTicketByTelegramId(long telegramId);
        Task<List<UserTicket>> GetTicketsByUsersCity(string city);
        Task<List<UserTicket>> GetTicketsByProfileCity(string city, int skip, int take);
        Task<int> CountTicketsByProfileCity(string city);
        Task<Guid?> GetTicketIdAtOffsetByProfileCity(string city, int offset);
        Task<List<string>> GetCitiesWithPendingTickets();
        Task RemoveUserTickets(Guid photoProfileId);
        Task<List<UserTicket>> GetUserTicketsByPhotoProfileId(Guid photoProfileId);
        Task<bool> ExistsByReporterAndProfile(Guid reporterUserId, Guid photoProfileId);
        Task<HashSet<Guid>> GetReportedPhotoProfileIdsByReporterAsync(
            Guid reporterUserId,
            CancellationToken cancellationToken = default);
        Task<string?> LockPhotoProfileAsync(
            Guid photoProfileId,
            CancellationToken cancellationToken = default);
        Task<bool> UnlockPhotoProfileAsync(
            Guid photoProfileId,
            string lockToken,
            CancellationToken cancellationToken = default);
        Task<bool> IsPhotoProfileLockedAsync(
            Guid photoProfileId,
            CancellationToken cancellationToken = default);
    }
}