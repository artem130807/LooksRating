using LooksRatingApi.Enums;
using LooksRatingApi.Models;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IPhotoProfileRepository
    {
        Task<PhotoProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<PhotoProfile?> GetByUserAndSeasonAsync(Guid userId, Guid seasonId, CancellationToken cancellationToken = default);
        Task<PhotoProfile?> GetByTelegramAndSeasonAsync(long telegramId, Guid seasonId, CancellationToken cancellationToken = default);
        Task<List<PhotoProfile>> GetByTelegramAndSeasonListAsync(long telegramId, Guid seasonId, CancellationToken cancellationToken = default);
        Task<List<PhotoProfile>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetTopProfileIdsAsync(
            Guid seasonId,
            bool seasonIsClosed,
            string cityNomination,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            bool vipOnly = false,
            CancellationToken cancellationToken = default);
        Task<int> CountTopProfilesAsync(
            Guid seasonId,
            bool seasonIsClosed,
            string cityNomination,
            GenderEnum gender,
            int age,
            bool vipOnly = false,
            CancellationToken cancellationToken = default);
        Task<int> CountFeedProfilesAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            CancellationToken cancellationToken = default);
        Task<List<Guid>> GetFeedCandidateProfileIdsAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            bool vipOnly = false,
            CancellationToken cancellationToken = default);
        Task<List<Guid>> GetNewFeedCandidateProfileIdsAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            DateTime createdAfter,
            int skip,
            int take,
            bool vipOnly = false,
            CancellationToken cancellationToken = default);
        Task<List<Guid>> GetRandomFeedCandidateProfileIdsAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            int take,
            bool vipOnly = false,
            CancellationToken cancellationToken = default);
        Task<List<Guid>> GetRandomNewFeedCandidateProfileIdsAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            DateTime createdAfter,
            int take,
            bool vipOnly = false,
            CancellationToken cancellationToken = default);
        Task<int> CountSeasonsWithProfileAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyDictionary<Guid, int>> GetParticipantCountsBySeasonIdsAsync(
            IEnumerable<Guid> seasonIds,
            CancellationToken cancellationToken = default);
        Task<List<PhotoProfile>> GetByUserIdWithSeasonAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<PhotoProfile>> GetByCitySnapshotAsync(Guid theBestWeekId, string city, int age, GenderEnum genderEnum);
        Task<List<Guid>> GetProfileIdsBatchAsync(Guid seasonId, int skip, int take, CancellationToken cancellationToken = default);
        Task ArchiveProfilesAsync(List<Guid> ids, CancellationToken cancellationToken = default);
        Task CreateAsync(PhotoProfile photoProfile, CancellationToken cancellationToken = default);
        Task<PhotoProfilePhoto?> AddPhotoAsync(Guid profileId, string telegramFileId, CancellationToken cancellationToken = default);
        Task UpdateAsync(PhotoProfile photoProfile, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns true when the user can add another photo in the current season profile.
        /// </summary>
        Task<bool> IsWithinVipPhotoLimitAsync(Guid seasonId, long telegramId, CancellationToken cancellationToken = default);
    }
}
