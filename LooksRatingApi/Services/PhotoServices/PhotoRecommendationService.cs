using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Enums;
using Microsoft.Extensions.Logging;

namespace LooksRatingApi.Services
{
    public class PhotoRecommendationService : IPhotoRecommendationService
    {
        private const int BatchSize = 50;
        private const int VipFeedInterval = 5;
        private const int MaxReservationAttempts = 5;

        private readonly IFeedCycleStore _feedCycleStore;
        private readonly INormalizeCityNameService _normalizeCityNameService;
        private readonly ICityService _cityService;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IUnviewablePhotosProfilesService _unviewablePhotosProfilesService;
        private readonly ILogger<PhotoRecommendationService> _logger;

        public PhotoRecommendationService(
            IFeedCycleStore feedCycleStore,
            INormalizeCityNameService normalizeCityNameService,
            ICityService cityService,
            ISeasonRepository seasonRepository,
            IPhotoProfileRepository photoProfileRepository,
            IReviewRepository reviewRepository,
            IUnviewablePhotosProfilesService unviewablePhotosProfilesService,
            ILogger<PhotoRecommendationService> logger)
        {
            _feedCycleStore = feedCycleStore;
            _normalizeCityNameService = normalizeCityNameService;
            _cityService = cityService;
            _seasonRepository = seasonRepository;
            _photoProfileRepository = photoProfileRepository;
            _reviewRepository = reviewRepository;
            _unviewablePhotosProfilesService = unviewablePhotosProfilesService;
            _logger = logger;
        }

        public async Task<Guid?> GetNextUnratedProfileIdAsync(
            Guid reviewerUserId,
            GenderEnum genderEnum,
            int age,
            string city,
            double? lastScore = null)
        {
            var ids = await GetNextUnratedProfileIdsAsync(reviewerUserId, genderEnum, age, city, lastScore);
            return ids.Count > 0 ? ids[0] : null;
        }

        public async Task<List<Guid>> GetNextUnratedProfileIdsAsync(
            Guid reviewerUserId,
            GenderEnum genderEnum,
            int age,
            string city,
            double? lastScore = null,
            IReadOnlyCollection<Guid>? skipProfileIds = null)
        {
            var season = await _seasonRepository.GetCurrent();
            if (season is null)
            {
                _logger.LogWarning(
                    "Feed empty: no active season for reviewer {ReviewerUserId}",
                    reviewerUserId);
                return new List<Guid>();
            }

            var feedCity = ResolveFeedCity(city);
            if (string.IsNullOrWhiteSpace(feedCity))
            {
                _logger.LogWarning(
                    "Feed empty: unresolved city '{City}' for reviewer {ReviewerUserId}",
                    city,
                    reviewerUserId);
                return new List<Guid>();
            }

            await _feedCycleStore.EnsureCycleAnchorAsync(reviewerUserId, season.Id);

            var ratedProfileIds = await _feedCycleStore.GetRatedProfileIdsAsync(reviewerUserId, season.Id);
            if (ratedProfileIds.Count == 0
                && !await _feedCycleStore.ShouldSkipRepairFromReviewsAsync(reviewerUserId, season.Id))
            {
                ratedProfileIds = await TryRepairRatedSetFromReviewsAsync(
                    reviewerUserId,
                    season.Id);
            }

            var unviewableProfileIds = await LoadUnviewableProfileIdsAsync(reviewerUserId);
            var skipIds = skipProfileIds?.ToHashSet() ?? new HashSet<Guid>();
            var completedRatings = await _feedCycleStore.GetFeedRatingCounterAsync(reviewerUserId, season.Id);

            var context = new FeedSearchContext(
                season.Id,
                reviewerUserId,
                feedCity,
                genderEnum,
                age,
                VipOnly: false);

            if (IsVipFeedTurn(completedRatings))
            {
                var vipContext = context with { VipOnly = true };
                var vipProfileId = await TryGetNextProfileAsync(
                    vipContext,
                    ratedProfileIds,
                    unviewableProfileIds,
                    skipIds);
                if (vipProfileId.HasValue)
                {
                    return [vipProfileId.Value];
                }
            }

            var profileId = await TryGetNextProfileAsync(
                context,
                ratedProfileIds,
                unviewableProfileIds,
                skipIds);
            if (profileId.HasValue)
            {
                return [profileId.Value];
            }

            var feedCount = await _photoProfileRepository.CountFeedProfilesAsync(
                season.Id,
                reviewerUserId,
                feedCity,
                genderEnum,
                age,
                excludeProfileIds: unviewableProfileIds);

            _logger.LogInformation(
                "Feed empty for reviewer {ReviewerUserId}: city={City}, gender={Gender}, age={Age}, season={SeasonId}, rated={RatedCount}, unviewable={UnviewableCount}, skipped={SkippedCount}, availableFeedCandidates={FeedCount}",
                reviewerUserId,
                feedCity,
                genderEnum,
                age,
                season.Id,
                ratedProfileIds.Count,
                unviewableProfileIds.Count,
                skipIds.Count,
                feedCount);

            return new List<Guid>();
        }

        private string ResolveFeedCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return string.Empty;
            }

            if (_cityService.TryResolveCanonicalCity(city, out var canonicalCity))
            {
                return canonicalCity;
            }

            return city.Trim().ToLowerInvariant();
        }

        private async Task<HashSet<Guid>> TryRepairRatedSetFromReviewsAsync(
            Guid reviewerUserId,
            Guid seasonId)
        {
            var reviewedProfileIds = await _reviewRepository.GetRatedPhotoProfileIdsForSeasonAsync(
                reviewerUserId,
                seasonId);
            if (reviewedProfileIds.Count == 0)
            {
                return new HashSet<Guid>();
            }

            await _feedCycleStore.AddRatedProfileIdsAsync(reviewerUserId, seasonId, reviewedProfileIds);
            return reviewedProfileIds.ToHashSet();
        }

        private async Task<Guid?> TryGetNextProfileAsync(
            FeedSearchContext context,
            HashSet<Guid> ratedProfileIds,
            HashSet<Guid> unviewableProfileIds,
            HashSet<Guid> skipProfileIds)
        {
            var reservedProfileId = await TryReserveProfileAsync(
                context,
                ratedProfileIds,
                unviewableProfileIds,
                skipProfileIds);
            if (reservedProfileId.HasValue)
            {
                return reservedProfileId;
            }

            if (await IsCycleCompleteAsync(
                    context,
                    ratedProfileIds,
                    unviewableProfileIds,
                    skipProfileIds))
            {
                if (skipProfileIds.Count > 0)
                {
                    _logger.LogWarning(
                        "Feed empty with transient skip list for reviewer {ReviewerUserId}: rated={RatedCount}, skipped={SkippedCount}",
                        context.ReviewerUserId,
                        ratedProfileIds.Count,
                        skipProfileIds.Count);
                    return null;
                }

                return await RestartCycleAndGetNextAsync(
                    context,
                    unviewableProfileIds,
                    skipProfileIds);
            }

            if (ratedProfileIds.Count > 0)
            {
                _logger.LogWarning(
                    "Feed empty mid-cycle for reviewer {ReviewerUserId}: rated={RatedCount}, cycle not complete",
                    context.ReviewerUserId,
                    ratedProfileIds.Count);
            }

            return null;
        }

        private async Task<Guid?> TryReserveProfileAsync(
            FeedSearchContext context,
            HashSet<Guid> ratedProfileIds,
            HashSet<Guid> unviewableProfileIds,
            HashSet<Guid> skipProfileIds)
        {
            for (var attempt = 0; attempt < MaxReservationAttempts; attempt++)
            {
                var excludeProfileIds = BuildExcludeProfileIds(
                    ratedProfileIds,
                    unviewableProfileIds,
                    skipProfileIds);

                var profileId = await TryGetByRandomOrderAsync(context, excludeProfileIds);
                if (!profileId.HasValue)
                {
                    return null;
                }

                if (await _feedCycleStore.TryMarkProfileAsServedAsync(
                        context.ReviewerUserId,
                        context.SeasonId,
                        profileId.Value))
                {
                    return profileId;
                }

                ratedProfileIds.Add(profileId.Value);
            }

            _logger.LogWarning(
                "Failed to reserve feed profile for reviewer {ReviewerUserId} after {AttemptCount} attempts",
                context.ReviewerUserId,
                MaxReservationAttempts);

            return null;
        }

        private static bool IsVipFeedTurn(int completedRatings) =>
            (completedRatings + 1) % VipFeedInterval == 0;

        private async Task<Guid?> RestartCycleAndGetNextAsync(
            FeedSearchContext context,
            HashSet<Guid> unviewableProfileIds,
            HashSet<Guid> skipProfileIds)
        {
            var previousAnchor = await _feedCycleStore.GetCycleAnchorAsync(
                context.ReviewerUserId,
                context.SeasonId);

            await _feedCycleStore.ResetCycleAsync(
                context.ReviewerUserId,
                context.SeasonId,
                DateTime.UtcNow);

            var reservedDuringRestart = new HashSet<Guid>();

            var newProfileId = await TryReserveProfileAfterRestartAsync(
                context,
                reservedDuringRestart,
                unviewableProfileIds,
                skipProfileIds,
                createdAfter: previousAnchor);
            if (newProfileId.HasValue)
            {
                return newProfileId;
            }

            return await TryReserveProfileAfterRestartAsync(
                context,
                reservedDuringRestart,
                unviewableProfileIds,
                skipProfileIds,
                createdAfter: null);
        }

        private async Task<Guid?> TryReserveProfileAfterRestartAsync(
            FeedSearchContext context,
            HashSet<Guid> reservedDuringRestart,
            HashSet<Guid> unviewableProfileIds,
            HashSet<Guid> skipProfileIds,
            DateTime? createdAfter)
        {
            for (var attempt = 0; attempt < MaxReservationAttempts; attempt++)
            {
                var excludeProfileIds = BuildExcludeProfileIds(
                    reservedDuringRestart,
                    unviewableProfileIds,
                    skipProfileIds);

                var profileId = createdAfter.HasValue
                    ? await TryGetByRandomOrderAsync(
                        context,
                        excludeProfileIds,
                        createdAfter: createdAfter.Value)
                    : await TryGetByRandomOrderAsync(context, excludeProfileIds);

                if (!profileId.HasValue)
                {
                    return null;
                }

                if (await _feedCycleStore.TryMarkProfileAsServedAsync(
                        context.ReviewerUserId,
                        context.SeasonId,
                        profileId.Value))
                {
                    return profileId;
                }

                reservedDuringRestart.Add(profileId.Value);
            }

            return null;
        }

        private async Task<Guid?> TryGetByRandomOrderAsync(
            FeedSearchContext context,
            IReadOnlyCollection<Guid> excludeProfileIds,
            DateTime? createdAfter = null)
        {
            var candidates = createdAfter.HasValue
                ? await _photoProfileRepository.GetRandomNewFeedCandidateProfileIdsAsync(
                    context.SeasonId,
                    context.ReviewerUserId,
                    context.CityNomination,
                    context.Gender,
                    context.Age,
                    createdAfter.Value,
                    BatchSize,
                    excludeProfileIds,
                    context.VipOnly)
                : await _photoProfileRepository.GetRandomFeedCandidateProfileIdsAsync(
                    context.SeasonId,
                    context.ReviewerUserId,
                    context.CityNomination,
                    context.Gender,
                    context.Age,
                    BatchSize,
                    excludeProfileIds,
                    context.VipOnly);

            return SelectRandomFromCandidates(candidates);
        }

        private static Guid? SelectRandomFromCandidates(IReadOnlyList<Guid> candidates)
        {
            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[Random.Shared.Next(candidates.Count)];
        }

        private static IReadOnlyCollection<Guid> BuildExcludeProfileIds(
            HashSet<Guid> ratedProfileIds,
            HashSet<Guid> unviewableProfileIds,
            HashSet<Guid> skipProfileIds)
        {
            var excludeProfileIds = new HashSet<Guid>(ratedProfileIds);
            excludeProfileIds.UnionWith(unviewableProfileIds);
            excludeProfileIds.UnionWith(skipProfileIds);
            return excludeProfileIds;
        }

        private async Task<bool> IsCycleCompleteAsync(
            FeedSearchContext context,
            HashSet<Guid> ratedProfileIds,
            HashSet<Guid> unviewableProfileIds,
            HashSet<Guid> skipProfileIds)
        {
            if (ratedProfileIds.Count == 0)
            {
                return false;
            }

            var excludedFromCycle = BuildCycleExcludedProfileIds(
                unviewableProfileIds,
                skipProfileIds);

            var availableFeedCount = await _photoProfileRepository.CountFeedProfilesAsync(
                context.SeasonId,
                context.ReviewerUserId,
                context.CityNomination,
                context.Gender,
                context.Age,
                excludeProfileIds: excludedFromCycle);

            if (availableFeedCount == 0)
            {
                return false;
            }

            var excludeProfileIds = BuildExcludeProfileIds(
                ratedProfileIds,
                unviewableProfileIds,
                skipProfileIds);

            var remainingFeedCount = await _photoProfileRepository.CountFeedProfilesAsync(
                context.SeasonId,
                context.ReviewerUserId,
                context.CityNomination,
                context.Gender,
                context.Age,
                excludeProfileIds: excludeProfileIds);

            return remainingFeedCount == 0;
        }

        private static IReadOnlyCollection<Guid> BuildCycleExcludedProfileIds(
            HashSet<Guid> unviewableProfileIds,
            HashSet<Guid> skipProfileIds)
        {
            var excludedFromCycle = new HashSet<Guid>(unviewableProfileIds);
            excludedFromCycle.UnionWith(skipProfileIds);
            return excludedFromCycle;
        }

        private async Task<HashSet<Guid>> LoadUnviewableProfileIdsAsync(Guid reviewerUserId)
        {
            var result = await _unviewablePhotosProfilesService.GetUnviewablePhotosProfile(reviewerUserId);
            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to load unviewable photo profiles for reviewer {ReviewerUserId}: {Error}",
                    reviewerUserId,
                    result.Error);
                return new HashSet<Guid>();
            }

            return result.Value.ToHashSet();
        }

        private sealed record FeedSearchContext(
            Guid SeasonId,
            Guid ReviewerUserId,
            string CityNomination,
            GenderEnum Gender,
            int Age,
            bool VipOnly);
    }
}
