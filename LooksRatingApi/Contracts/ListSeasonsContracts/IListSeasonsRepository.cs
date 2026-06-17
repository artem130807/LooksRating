using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.ListSeasonsContracts
{
    public interface IListSeasonsRepository
    {
        Task Create(ListSeasons listSeasons);
        Task Delete(Guid id);
        Task Update(ListSeasons listSeasons);
        Task<ListSeasons?> GetById(Guid id);
        Task<ListSeasons?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<ListSeasons?> GetLatest(bool includeSeasons = true);
        Task<ListSeasons?> GetLatestAsync(bool includeSeasons = true, CancellationToken cancellationToken = default);
        Task<ListSeasons?> GetPreviousToLatest();
        Task<List<ListSeasons>> GetLists(bool includeSeasons = false);
        Task<List<ListSeasons>> GetListsAsync(bool includeSeasons = false, CancellationToken cancellationToken = default);
    }
}

