using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services.PhotoProfiles;
using LooksRatingApi.Services;
using LooksRatingApi.Services.TheBestWeek;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public sealed class PhotoProfileRepository : IPhotoProfileRepository
    {
        private readonly LooksRatingDbContext _context;

        public PhotoProfileRepository(LooksRatingDbContext context)
        {
            _context = context;
        }

        public async Task<PhotoProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.PhotoProfiles
                .Include(x => x.User)
                .Include(x => x.Photos.OrderBy(p => p.SortOrder))
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<PhotoProfile?> GetByUserAndSeasonAsync(Guid userId, Guid seasonId, CancellationToken cancellationToken = default)
        {
            return await _context.PhotoProfiles
                .Include(x => x.User)
                .Include(x => x.Photos.OrderBy(p => p.SortOrder))
                .FirstOrDefaultAsync(x => x.UserId == userId && x.SeasonId == seasonId, cancellationToken);
        }

        public async Task<PhotoProfile?> GetByTelegramAndSeasonAsync(long telegramId, Guid seasonId, CancellationToken cancellationToken = default)
        {
            return await _context.PhotoProfiles
                .Include(x => x.User)
                .Include(x => x.Photos.OrderBy(p => p.SortOrder))
                .FirstOrDefaultAsync(x => x.User.TelegramId == telegramId && x.SeasonId == seasonId, cancellationToken);
        }

        public async Task<List<PhotoProfile>> GetByTelegramAndSeasonListAsync(long telegramId, Guid seasonId, CancellationToken cancellationToken = default)
        {
            return await _context.PhotoProfiles
                .Include(x => x.User)
                .Include(x => x.Photos.OrderBy(p => p.SortOrder))
                .Where(x => x.User.TelegramId == telegramId && x.SeasonId == seasonId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<PhotoProfile>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
        {
            if (ids.Count == 0)
            {
                return new List<PhotoProfile>();
            }

            return await _context.PhotoProfiles
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.Photos.OrderBy(p => p.SortOrder))
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetTopProfileIdsAsync(
            Guid seasonId,
            bool seasonIsClosed,
            string cityNomination,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            bool vipOnly = false,
            CancellationToken cancellationToken = default)
        {
            var statuses = seasonIsClosed
                ? new[] { StatusEnum.Active, StatusEnum.Archived }
                : new[] { StatusEnum.Active };

            var query = BuildTopQuery(seasonId, statuses, cityNomination, gender, age, vipOnly);

            return await query
                .Select(p => new
                {
                    p.Id,
                    p.Rating,
                    p.RatingCount,
                    p.CreatedAt,
                    HasVotes = p.RatingCount > 0 ? 1 : 0,
                    Score = p.RatingCount > 0
                        ? ((p.Rating * p.RatingCount) + (PhotoRankingScore.PriorMean * PhotoRankingScore.PriorWeight))
                            / (p.RatingCount + PhotoRankingScore.PriorWeight)
                        : PhotoRankingScore.UnratedScore,
                })
                .OrderByDescending(p => p.HasVotes)
                .ThenByDescending(p => p.Score)
                .ThenByDescending(p => p.Rating)
                .ThenByDescending(p => p.RatingCount)
                .ThenByDescending(p => p.CreatedAt)
                .ThenBy(p => p.Id)
                .Skip(skip)
                .Take(take)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountTopProfilesAsync(
            Guid seasonId,
            bool seasonIsClosed,
            string cityNomination,
            GenderEnum gender,
            int age,
            bool vipOnly = false,
            CancellationToken cancellationToken = default)
        {
            var statuses = seasonIsClosed
                ? new[] { StatusEnum.Active, StatusEnum.Archived }
                : new[] { StatusEnum.Active };

            var query = BuildTopQuery(seasonId, statuses, cityNomination, gender, age, vipOnly);
            return await query.CountAsync(cancellationToken);
        }

        public async Task<SeasonTopPosition?> GetSeasonTopPositionAsync(
            PhotoProfile profile,
            bool seasonIsClosed,
            CancellationToken cancellationToken = default)
        {
            var query = BuildTopQueryForProfile(profile, seasonIsClosed, vipOnly: false);
            if (query is null)
            {
                return null;
            }

            if (!await query.AnyAsync(candidate => candidate.Id == profile.Id, cancellationToken))
            {
                return null;
            }

            var aboveCount = await CountProfilesRankedAboveAsync(query, profile, cancellationToken);
            var totalCount = await query.CountAsync(cancellationToken);

            return new SeasonTopPosition(aboveCount + 1, totalCount);
        }

        private IQueryable<PhotoProfile>? BuildTopQueryForProfile(
            PhotoProfile profile,
            bool seasonIsClosed,
            bool vipOnly)
        {
            var city = profile.CityNomination?.Value;
            if (string.IsNullOrWhiteSpace(city))
            {
                return null;
            }

            var ageBracket = TopService.GetTop(profile.AgeNomination);
            if (ageBracket.Length == 0)
            {
                return null;
            }

            var statuses = seasonIsClosed
                ? new[] { StatusEnum.Active, StatusEnum.Archived }
                : new[] { StatusEnum.Active };

            return BuildTopQuery(
                profile.SeasonId,
                statuses,
                city,
                profile.GenderNomination,
                ageBracket[0],
                vipOnly);
        }

        private static async Task<int> CountProfilesRankedAboveAsync(
            IQueryable<PhotoProfile> query,
            PhotoProfile profile,
            CancellationToken cancellationToken)
        {
            var myHasVotes = profile.RatingCount > 0 ? 1 : 0;
            var priorMean = PhotoRankingScore.PriorMean;
            var priorWeight = PhotoRankingScore.PriorWeight;
            var myScore = profile.RatingCount > 0
                ? ((profile.Rating * profile.RatingCount) + (priorMean * priorWeight))
                    / (profile.RatingCount + priorWeight)
                : PhotoRankingScore.UnratedScore;

            return await query
                .Where(candidate => candidate.Id != profile.Id)
                .CountAsync(
                    candidate =>
                        (candidate.RatingCount > 0 ? 1 : 0) > myHasVotes
                        || ((candidate.RatingCount > 0 ? 1 : 0) == myHasVotes
                            && (
                                (candidate.RatingCount > 0
                                    ? ((candidate.Rating * candidate.RatingCount) + (priorMean * priorWeight))
                                        / (candidate.RatingCount + priorWeight)
                                    : PhotoRankingScore.UnratedScore) > myScore
                                || ((candidate.RatingCount > 0
                                    ? ((candidate.Rating * candidate.RatingCount) + (priorMean * priorWeight))
                                        / (candidate.RatingCount + priorWeight)
                                    : PhotoRankingScore.UnratedScore) == myScore
                                    && candidate.Rating > profile.Rating)
                                || ((candidate.RatingCount > 0
                                    ? ((candidate.Rating * candidate.RatingCount) + (priorMean * priorWeight))
                                        / (candidate.RatingCount + priorWeight)
                                    : PhotoRankingScore.UnratedScore) == myScore
                                    && candidate.Rating == profile.Rating
                                    && candidate.RatingCount > profile.RatingCount)
                                || ((candidate.RatingCount > 0
                                    ? ((candidate.Rating * candidate.RatingCount) + (priorMean * priorWeight))
                                        / (candidate.RatingCount + priorWeight)
                                    : PhotoRankingScore.UnratedScore) == myScore
                                    && candidate.Rating == profile.Rating
                                    && candidate.RatingCount == profile.RatingCount
                                    && candidate.CreatedAt > profile.CreatedAt)
                                || ((candidate.RatingCount > 0
                                    ? ((candidate.Rating * candidate.RatingCount) + (priorMean * priorWeight))
                                        / (candidate.RatingCount + priorWeight)
                                    : PhotoRankingScore.UnratedScore) == myScore
                                    && candidate.Rating == profile.Rating
                                    && candidate.RatingCount == profile.RatingCount
                                    && candidate.CreatedAt == profile.CreatedAt
                                    && candidate.Id.CompareTo(profile.Id) < 0))),
                    cancellationToken);
        }

        public async Task<int> CountFeedProfilesAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            CancellationToken cancellationToken = default)
        {
            return await BuildFeedQuery(seasonId, reviewerUserId, cityNomination, gender, age)
                .CountAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetFeedCandidateProfileIdsAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            bool vipOnly = false,
            CancellationToken cancellationToken = default)
        {
            return await BuildFeedQuery(seasonId, reviewerUserId, cityNomination, gender, age, vipOnly)
                .Select(p => new
                {
                    p.Id,
                    p.Rating,
                    p.RatingCount,
                    p.CreatedAt,
                    HasVotes = p.RatingCount > 0 ? 1 : 0,
                    Score = p.RatingCount > 0
                        ? ((p.Rating * p.RatingCount) + (PhotoRankingScore.PriorMean * PhotoRankingScore.PriorWeight))
                            / (p.RatingCount + PhotoRankingScore.PriorWeight)
                        : PhotoRankingScore.UnratedScore,
                })
                .OrderByDescending(p => p.HasVotes)
                .ThenByDescending(p => p.Score)
                .ThenByDescending(p => p.Rating)
                .ThenByDescending(p => p.RatingCount)
                .ThenByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetNewFeedCandidateProfileIdsAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            DateTime createdAfter,
            int skip,
            int take,
            bool vipOnly = false,
            CancellationToken cancellationToken = default)
        {
            return await BuildFeedQuery(seasonId, reviewerUserId, cityNomination, gender, age, vipOnly)
                .Where(p => p.CreatedAt > createdAfter)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetRandomFeedCandidateProfileIdsAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            int take,
            bool vipOnly = false,
            CancellationToken cancellationToken = default)
        {
            return await BuildFeedQuery(seasonId, reviewerUserId, cityNomination, gender, age, vipOnly)
                .OrderBy(_ => EF.Functions.Random())
                .Take(take)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetRandomNewFeedCandidateProfileIdsAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            DateTime createdAfter,
            int take,
            bool vipOnly = false,
            CancellationToken cancellationToken = default)
        {
            return await BuildFeedQuery(seasonId, reviewerUserId, cityNomination, gender, age, vipOnly)
                .Where(p => p.CreatedAt > createdAfter)
                .OrderBy(_ => EF.Functions.Random())
                .Take(take)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountSeasonsWithProfileAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.PhotoProfiles
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => p.SeasonId)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        public async Task<IReadOnlyDictionary<Guid, int>> GetParticipantCountsBySeasonIdsAsync(
            IEnumerable<Guid> seasonIds,
            CancellationToken cancellationToken = default)
        {
            var ids = seasonIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, int>();

            return await _context.PhotoProfiles
                .AsNoTracking()
                .Where(p => ids.Contains(p.SeasonId))
                .Where(p => p.Status == StatusEnum.Active || p.Status == StatusEnum.Archived)
                .GroupBy(p => p.SeasonId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        }

        public async Task<List<PhotoProfile>> GetByUserIdWithSeasonAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.PhotoProfiles
                .Include(p => p.Season)
                .Include(p => p.Photos.OrderBy(x => x.SortOrder))
                .Where(p => p.UserId == userId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<PhotoProfile>> GetByCitySnapshotAsync(Guid theBestWeekId, string city, int age, GenderEnum genderEnum)
        {
            var theBestWeek = await _context.TheBestWeeks.FirstOrDefaultAsync(x => x.Id == theBestWeekId && x.City == city);
            if (theBestWeek == null)
            {
                return new List<PhotoProfile>();
            }

            var snapshotItems = TheBestWeekSnapshotSerializer.Deserialize(theBestWeek.SnapshotJson);

            return snapshotItems
                .Select(TheBestWeekSnapshotSerializer.ToProfile)
                .Where(p => TopService.MatchesAge(age, p.AgeNomination) && GenderFeedHelper.Matches(genderEnum, p.GenderNomination))
                .OrderByDescending(x => x.RatingCount > 0 ? 1 : 0)
                .ThenByDescending(x => PhotoRankingScore.ToRankScore(x.Rating, x.RatingCount))
                .ThenByDescending(x => x.Rating)
                .ThenByDescending(x => x.RatingCount)
                .ThenByDescending(x => x.CreatedAt)
                .Take(10)
                .ToList();
        }

        public async Task<List<Guid>> GetProfileIdsBatchAsync(Guid seasonId, int skip, int take, CancellationToken cancellationToken = default)
        {
            return await _context.PhotoProfiles
                .AsNoTracking()
                .Where(p => p.SeasonId == seasonId && p.Status == StatusEnum.Active)
                .OrderBy(p => p.Id)
                .Skip(skip)
                .Take(take)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task ArchiveProfilesAsync(List<Guid> ids, CancellationToken cancellationToken = default)
        {
            await _context.PhotoProfiles
                .Where(p => ids.Contains(p.Id))
                .ExecuteUpdateAsync(
                    setter => setter.SetProperty(p => p.Status, StatusEnum.Archived),
                    cancellationToken);
        }

        public async Task CreateAsync(PhotoProfile photoProfile, CancellationToken cancellationToken = default)
        {
            _context.PhotoProfiles.Add(photoProfile);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<PhotoProfilePhoto?> AddPhotoAsync(
            Guid profileId,
            string telegramFileId,
            CancellationToken cancellationToken = default)
        {
            var profile = await _context.PhotoProfiles
                .Include(x => x.User)
                .Include(x => x.Photos)
                .FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken);

            if (profile is null)
            {
                return null;
            }

            if (!PhotoProfileLimits.CanAddPhoto(profile.Photos.Count, profile.User.Status))
            {
                throw new InvalidOperationException(SetUserPhotoErrors.VipPhotoLimitExceeded);
            }

            var maxSortOrder = profile.Photos.Count == 0
                ? -1
                : profile.Photos.Max(p => p.SortOrder);

            var photo = new PhotoProfilePhoto
            {
                Id = Guid.NewGuid(),
                PhotoProfileId = profileId,
                TelegramFileId = telegramFileId,
                SortOrder = maxSortOrder + 1,
                CreatedAt = DateTime.UtcNow,
            };

            _context.PhotoProfilePhotos.Add(photo);

            if (profile.Status != StatusEnum.Active)
            {
                await _context.PhotoProfiles
                    .Where(x => x.Id == profileId)
                    .ExecuteUpdateAsync(
                        setter => setter.SetProperty(p => p.Status, StatusEnum.Active),
                        cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return photo;
        }

        public async Task UpdateAsync(PhotoProfile photoProfile, CancellationToken cancellationToken = default)
        {
            if (_context.Entry(photoProfile).State == EntityState.Detached)
            {
                _context.PhotoProfiles.Update(photoProfile);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _context.PhotoProfiles
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(cancellationToken);
        }

        public async Task<bool> IsWithinVipPhotoLimitAsync(Guid seasonId, long telegramId, CancellationToken cancellationToken = default)
        {
            var profile = await GetByTelegramAndSeasonAsync(telegramId, seasonId, cancellationToken);
            if (profile is null)
            {
                return true;
            }

            return PhotoProfileLimits.CanAddPhoto(profile.Photos.Count, profile.User.Status);
        }

        private IQueryable<PhotoProfile> BuildTopQuery(
            Guid seasonId,
            IReadOnlyCollection<StatusEnum> statuses,
            string cityNomination,
            GenderEnum gender,
            int age,
            bool vipOnly = false)
        {
            var topAge = TopService.GetTop(age);
            var query = _context.PhotoProfiles
                .AsNoTracking()
                .Where(p => p.SeasonId == seasonId)
                .Where(p => statuses.Contains(p.Status))
                .Where(p => p.CityNomination.Value == cityNomination);

            if (vipOnly)
            {
                query = query.Where(p => p.User.Status == VipStatus.Availlable);
            }

            query = GenderFeedHelper.ApplyFilter(query, gender);

            if (age == TopService.AllAges)
            {
                return query;
            }

            if (topAge.Length == 0)
            {
                return query.Where(p => false);
            }

            return query.Where(p =>
                p.AgeNomination == topAge[0]
                || p.AgeNomination == topAge[1]
                || p.AgeNomination == topAge[2]);
        }

        private IQueryable<PhotoProfile> BuildFeedQuery(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            bool vipOnly = false)
        {
            var topAge = TopService.GetTop(age);
            var query = _context.PhotoProfiles
                .AsNoTracking()
                .Where(p => p.SeasonId == seasonId)
                .Where(p => p.Status == StatusEnum.Active)
                .Where(p => p.UserId != reviewerUserId)
                .Where(p => p.CityNomination.Value == cityNomination)
                .Where(p => p.Photos.Any(photo => !string.IsNullOrEmpty(photo.TelegramFileId)));

            if (vipOnly)
            {
                query = query.Where(p => p.User.Status == VipStatus.Availlable);
            }

            query = GenderFeedHelper.ApplyFilter(query, gender);

            if (age == TopService.AllAges)
            {
                return query;
            }

            if (topAge.Length == 0)
            {
                return query.Where(p => false);
            }

            return query.Where(p =>
                p.AgeNomination == topAge[0]
                || p.AgeNomination == topAge[1]
                || p.AgeNomination == topAge[2]);
        }
    }
}
