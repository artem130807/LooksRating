using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Repositories
{
    public class PhotoUserRepository : IPhotoUserRepository
    {
        private readonly LooksRatingDbContext _context;
        public PhotoUserRepository(LooksRatingDbContext context)
        {
            _context = context;
        }
        public async Task Create(PhotoUser photoUser)
        {
            _context.PhotoUsers.Add(photoUser);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(Guid Id)
        {
            await _context.PhotoUsers.Where(x => x.Id == Id).ExecuteDeleteAsync();
        }

        public async Task<PhotoUser?> GePhotoUserById(Guid Id)
        {
            return await _context.PhotoUsers
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == Id);
        }

        public async Task<List<PhotoUser>> GetByProfileIdAsync(Guid photoProfileId, CancellationToken cancellationToken = default)
        {
            return await _context.PhotoUsers
                .Where(p => p.PhotoProfileId == photoProfileId)
                .ToListAsync(cancellationToken);
        }

        public async Task ResetLegacyRatingsForProfileAsync(
            Guid photoProfileId,
            CancellationToken cancellationToken = default)
        {
            await _context.PhotoUsers
                .Where(x => x.PhotoProfileId == photoProfileId)
                .ExecuteUpdateAsync(
                    setter => setter
                        .SetProperty(x => x.Rating, 0m)
                        .SetProperty(x => x.RatingCount, 0)
                        .SetProperty(x => x.Rank, RankEnum.Terrible),
                    cancellationToken);
        }

        public async Task<List<PhotoUser>> GetByIdsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            if (ids.Count == 0)
            {
                return new List<PhotoUser>();
            }

            return await _context.PhotoUsers
                .AsNoTracking()
                .Include(p => p.User)
                .Where(p => ids.Contains(p.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<PhotoUser?> GetByTelegramIdAndSeasonIdAsync(
            long telegramId,
            Guid seasonId,
            CancellationToken cancellationToken = default)
        {
            return await _context.PhotoUsers
                .Include(x => x.User)
                .Where(x => x.User.TelegramId == telegramId && x.SeasonId == seasonId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<PhotoUser>> GetByTelegramIdAndSeasonIdListAsync(
            long telegramId,
            Guid seasonId,
            CancellationToken cancellationToken = default)
        {
            return await _context.PhotoUsers
                .Include(x => x.User)
                .Where(x => x.User.TelegramId == telegramId && x.SeasonId == seasonId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetActivePhotoIdsByUserAndSeasonAsync(
            Guid userId,
            Guid seasonId,
            CancellationToken cancellationToken = default)
        {
            return await _context.PhotoUsers
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.SeasonId == seasonId && x.Status == StatusEnum.Active)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        // public async Task<List<PhotoUser>> GetPhotoUsers(Guid userId, PhotoFilter filter)
        // {
        //     return await _context.PhotoUsers.Include(x => x.User)
        //     .Where(p => p.UserId != userId && p.User.City.Value == filter.City)
        //     .Where(p => p.User.Age == filter.Age || p.User.Age == filter.Age - 1 || p.User.Age == filter.Age + 1)
        //     .ToListAsync();
        // }
        public async Task<List<PhotoUser>> GetPhotoUsers()
        {
            return await _context.PhotoUsers.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<List<PhotoUser>> GetByUserIdWithSeasonAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.PhotoUsers
                .Include(p => p.Season)
                .Where(p => p.UserId == userId)
                .ToListAsync(cancellationToken);
        }
        public async Task<(List<PhotoUser> Items, int TotalCount)> GetTopPhotosPagedAsync(
            Guid seasonId,
            bool seasonIsClosed,
            string cityNomination,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var topAge = TopService.GetTop(age);
            if (age != TopService.AllAges && topAge.Length == 0)
            {
                return (new List<PhotoUser>(), 0);
            }
            var statuses = seasonIsClosed
                ? new[] { Enums.StatusEnum.Active, Enums.StatusEnum.Archived }
                : new[] { Enums.StatusEnum.Active };

            var query = _context.PhotoUsers
                .AsNoTracking()
                .Include(p => p.User)
                .Where(p => p.SeasonId == seasonId)
                .Where(p => statuses.Contains(p.Status))
                .Where(p => p.CityNomination.Value == cityNomination);

            query = GenderFeedHelper.ApplyFilter(query, gender);

            if (age != TopService.AllAges)
            {
                query = query
                    .Where(p => p.AgeNomination == topAge[0]
                        || p.AgeNomination == topAge[1]
                        || p.AgeNomination == topAge[2]);
            }

            var total = await query.CountAsync(cancellationToken);
            var scoredItems = await query
                .Select(p => new
                {
                    Photo = p,
                    HasVotes = p.RatingCount > 0 ? 1 : 0,
                    Score = p.RatingCount > 0
                        ? ((p.Rating * p.RatingCount) + (PhotoRankingScore.PriorMean * PhotoRankingScore.PriorWeight))
                            / (p.RatingCount + PhotoRankingScore.PriorWeight)
                        : PhotoRankingScore.UnratedScore,
                })
                .OrderByDescending(x => x.HasVotes)
                .ThenByDescending(x => x.Score)
                .ThenByDescending(x => x.Photo.Rating)
                .ThenByDescending(x => x.Photo.RatingCount)
                .ThenByDescending(x => x.Photo.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
            var items = scoredItems.Select(x => x.Photo).ToList();

            return (items, total);
        }

        public async Task<int> CountSeasonsWithPhotoAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.PhotoUsers
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => p.SeasonId)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        public async Task<int> CountFeedPhotosAsync(
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

        public async Task<List<Guid>> GetFeedCandidateIdsAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            return await BuildFeedQuery(seasonId, reviewerUserId, cityNomination, gender, age)
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

        public async Task<List<Guid>> GetNewFeedCandidateIdsAsync(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age,
            DateTime createdAfter,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            return await BuildFeedQuery(seasonId, reviewerUserId, cityNomination, gender, age)
                .Where(p => p.CreatedAt > createdAfter)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Select(p => p.Id)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        private IQueryable<PhotoUser> BuildFeedQuery(
            Guid seasonId,
            Guid reviewerUserId,
            string cityNomination,
            GenderEnum gender,
            int age)
        {
            var topAge = TopService.GetTop(age);
            var query = _context.PhotoUsers
                .AsNoTracking()
                .Where(p => p.SeasonId == seasonId)
                .Where(p => p.Status == Enums.StatusEnum.Active)
                .Where(p => p.UserId != reviewerUserId)
                .Where(p => p.CityNomination.Value == cityNomination);

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

        public async Task<List<PhotoUser>> GetTopActivePhotosByCityAsync(
            string city,
            int take,
            DateTime periodStart,
            DateTime periodEnd,
            CancellationToken cancellationToken)
        {
            var scoredItems = await _context.PhotoUsers
                .AsNoTracking()
                .Include(p => p.User)
                .Where(p => p.Status == Enums.StatusEnum.Active
                    && p.CityNomination.Value == city
                    && p.CreatedAt >= periodStart
                    && p.CreatedAt < periodEnd)
                .Select(p => new
                {
                    Photo = p,
                    HasVotes = p.RatingCount > 0 ? 1 : 0,
                    Score = p.RatingCount > 0
                        ? ((p.Rating * p.RatingCount) + (PhotoRankingScore.PriorMean * PhotoRankingScore.PriorWeight))
                            / (p.RatingCount + PhotoRankingScore.PriorWeight)
                        : PhotoRankingScore.UnratedScore,
                })
                .OrderByDescending(x => x.HasVotes)
                .ThenByDescending(x => x.Score)
                .ThenByDescending(x => x.Photo.Rating)
                .ThenByDescending(x => x.Photo.RatingCount)
                .ThenByDescending(x => x.Photo.CreatedAt)
                .Take(take)
                .ToListAsync(cancellationToken);
            return scoredItems.Select(x => x.Photo).ToList();
        }

        public async Task<List<Guid>> GetPhotoIdsBatch(Guid seasonId, int skip, int take)
        {
            return await _context.PhotoUsers
                .Where(p => p.SeasonId == seasonId && p.Status == Enums.StatusEnum.Active)
                .OrderBy(p => p.Id)
                .Skip(skip)
                .Take(take)
                .Select(p => p.Id)
                .ToListAsync();
        }
        public async Task ExecuteUpdateAsync(List<Guid> ids)
        { 
            await _context.PhotoUsers
            .Where(p => ids.Contains(p.Id))
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(p => p.Status, Enums.StatusEnum.Archived)
            );
        }
        public async Task<List<Guid>> GetPhotoUsersId()
        {
            var photoUsers = await _context.PhotoUsers.OrderByDescending(p => p.CreatedAt).ToListAsync();
            return photoUsers.Select(p => p.Id).ToList();
        }
        public async Task Update(PhotoUser photoUser)
        {
            _context.PhotoUsers.Update(photoUser);
            await _context.SaveChangesAsync();
        }
        public async Task<List<PhotoUser>> GetByCityAsync(
            Guid theBestWeekId,
            string city,
            int age,
            GenderEnum genderEnum)
        {
            var theBestWeek = await _context.TheBestWeeks.FirstOrDefaultAsync(x => x.Id == theBestWeekId && x.City == city);
            if(theBestWeek == null)
                return new List<PhotoUser>();
            var photosJson = theBestWeek.SnapshotJson;
            var photos = JsonSerializer.Deserialize<List<PhotoUser>>(photosJson);
            if(photos == null)
                return new List<PhotoUser>();
            return photos.Where(p => TopService.MatchesAge(age, p.AgeNomination))
            .OrderByDescending(x => x.RatingCount > 0 ? 1 : 0)
            .ThenByDescending(x => PhotoRankingScore.ToRankScore(x.Rating, x.RatingCount))
            .ThenByDescending(x => x.Rating)
            .ThenByDescending(x => x.RatingCount)
            .ThenByDescending(x => x.CreatedAt)
            .Take(10)
            .ToList();
        }
    }
}