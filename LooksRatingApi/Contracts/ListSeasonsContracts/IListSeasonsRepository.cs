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
        Task<ListSeasons?> GetLatest(bool includeSeasons = true);
        Task<List<ListSeasons>> GetLists(bool includeSeasons = false);
    }
}

