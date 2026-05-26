using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.SeasonContracts
{
    public interface ISeasonRepository
    {
        Task Create(Season season);
        Task Delete(Guid id);
        Task Update(Season season);
        Task<Season?> GetById(Guid id);
        Task<Season?> GetByIdWithChapterAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Season?> GetByNumber(int number);
        Task<Season?> GetCurrent();
        Task<Season?> GetCurrentByList(Guid listId);
        Task<List<Season>> GetSeasons(bool includeClosed = true);
        Task<List<Season>> GetByListSeasonsIdAsync(Guid listSeasonsId, bool includeClosed = true, CancellationToken cancellationToken = default);
        Task<Dictionary<Guid, int>> GetPhotoCountsBySeasonIdsAsync(IEnumerable<Guid> seasonIds, CancellationToken cancellationToken = default);
    }
}

