using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IPhotoUserRepository
    {
        Task Create(PhotoUser photoUser);
        Task Delete(Guid Id);
        Task Update(PhotoUser photoUser);
        Task<PhotoUser?> GePhotoUserById(Guid Id);
        Task<List<PhotoUser>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);
        Task<PhotoUser?> GetByTelegramIdAndSeasonIdAsync(long telegramId, Guid seasonId, CancellationToken cancellationToken = default);
        Task<List<PhotoUser>> GetPhotoUsers();
        Task<List<PhotoUser>> GetByUserIdWithSeasonAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
        Task<List<Guid>> GetPhotoIdsBatch(Guid seasonId, int skip, int take);
        Task<List<PhotoUser>> GetTopActivePhotosByCityAsync(
            string city,
            int take,
            DateTime periodStart,
            DateTime periodEnd,
            CancellationToken cancellationToken);
        Task<(List<PhotoUser> Items, int TotalCount)> GetTopPhotosPagedAsync(
            Guid seasonId,
            bool seasonIsClosed,
            string normalizedCity,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            CancellationToken cancellationToken = default);
        Task<int> CountSeasonsWithPhotoAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<int> CountFeedPhotosAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            CancellationToken cancellationToken = default);
        Task<List<Guid>> GetFeedCandidateIdsAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            CancellationToken cancellationToken = default);
        Task<List<Guid>> GetNewFeedCandidateIdsAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            DateTime createdAfter,
            int skip,
            int take,
            CancellationToken cancellationToken = default);
        Task<List<Guid>> GetPhotoUsersId();
        Task ExecuteUpdateAsync(List<Guid> ids);
        Task<List<PhotoUser>> GetByCityAsync(
            Guid theBestWeekId,
            string city,
            int age,
            GenderEnum genderEnum);
    }
}