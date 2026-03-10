using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.TopPhotoSeasonContracts
{
    public interface ITopPhotoSeasonRepository
    {
        Task Create(TopPhotoSeason topPhotoSeason);
        Task Delete(Guid id);
        Task Update(TopPhotoSeason topPhotoSeason);

        Task<TopPhotoSeason?> GetById(Guid id);
        Task<TopPhotoSeason?> GetByPhotoSeasonAndGender(Guid photoSeasonId, GenderEnum gender);
        Task<List<TopPhotoSeason>> GetTopBySeason(Guid seasonId, string city, GenderEnum gender, int take);
    }
}

