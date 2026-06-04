using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTopUserPhotos
{
    public sealed class GetTopUserPhotosHandler
        : IRequestHandler<GetTopUserPhotosQuery, Result<GetTopUserPhotosPagedResponse>>
    {
        private readonly IGetTopUserPhotosValidator _validator;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IPhotoTopReadService _photoTopReadService;

        public GetTopUserPhotosHandler(
            IGetTopUserPhotosValidator validator,
            IPhotoProfileRepository photoProfileRepository,
            IPhotoTopReadService photoTopReadService)
        {
            _validator = validator;
            _photoProfileRepository = photoProfileRepository;
            _photoTopReadService = photoTopReadService;
        }

        public async Task<Result<GetTopUserPhotosPagedResponse>> Handle( GetTopUserPhotosQuery query,CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(query, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<GetTopUserPhotosPagedResponse>(validationResult.Error);
            }

            var context = validationResult.Value;
            var (profileIds, total) = await _photoTopReadService.GetTopProfileIdsAsync(
                context.Season.Id,
                context.Season.IsClosed,
                context.FeedCity,
                context.Query.Gender,
                context.Query.Age,
                context.Skip,
                context.PageSize,
                cancellationToken: cancellationToken);

            var profiles = await _photoProfileRepository.GetByIdsAsync(profileIds, cancellationToken);
            var profilesById = profiles.ToDictionary(p => p.Id);

            var items = new List<GetTopUserPhotosResponse>(profileIds.Count);
            foreach (var profileId in profileIds)
            {
                if (!profilesById.TryGetValue(profileId, out var profile))
                {
                    continue;
                }

                var files = profile.Photos
                    .OrderBy(x => x.SortOrder)
                    .Select(x => x.TelegramFileId)
                    .ToList();
                var firstFile = files.FirstOrDefault() ?? string.Empty;

                items.Add(new GetTopUserPhotosResponse
                {
                    Id = profile.Id,
                    ProfileId = profile.Id,
                    Place = context.Skip + items.Count + 1,
                    Name = UserPublicDisplayName.Resolve(profile.User),
                    TelegramFileId = firstFile,
                    TelegramFileIds = files,
                    Rating = profile.Rating,
                    RatingCount = profile.RatingCount,
                    GenderNomination = GenderDisplay.GetGender(profile.GenderNomination),
                    AgeNomination = profile.AgeNomination,
                });
            }

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)context.PageSize);

            return Result.Success(new GetTopUserPhotosPagedResponse
            {
                Items = items,
                TotalCount = total,
                Page = context.Page,
                PageSize = context.PageSize,
                TotalPages = totalPages,
                SeasonId = context.Season.Id,
                SeasonName = context.Season.Name,
                SeasonNumber = context.Season.Number,
                IsCurrentSeason = context.CurrentSeason?.Id == context.Season.Id,
                IsClosed = context.Season.IsClosed,
            });
        }
    }
}
