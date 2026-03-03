using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.PhotoSeasonContracts
{
    public interface IPhotoSeasonRepository
    {
        Task Create(PhotoSeason photoSeason);
        Task Delete(Guid id);
        Task Update(PhotoSeason photoSeason);

        Task<PhotoSeason?> GetById(Guid id);
        Task<PhotoSeason?> GetBySeasonAndUser(Guid seasonId, Guid userId);
        Task<List<PhotoSeason>> GetBySeason(Guid seasonId);
        Task<List<PhotoSeason>> GetTopBySeason(Guid seasonId, int take);
    }
}

