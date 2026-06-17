using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IPhotoRecommendationService
    {
        Task<Guid?> GetNextUnratedProfileIdAsync(
            Guid reviewerUserId,
            GenderEnum genderEnum,
            int age,
            string city,
            double? lastScore = null);

        Task<List<Guid>> GetNextUnratedProfileIdsAsync(
            Guid reviewerUserId,
            GenderEnum genderEnum,
            int age,
            string city,
            double? lastScore = null,
            IReadOnlyCollection<Guid>? skipProfileIds = null);
    }
}