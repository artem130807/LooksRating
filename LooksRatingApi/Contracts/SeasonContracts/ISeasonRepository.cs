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
        Task<Season?> GetByNumber(int number);
        Task<Season?> GetCurrent();
        Task<List<Season>> GetSeasons(bool includeClosed = true);
    }
}

