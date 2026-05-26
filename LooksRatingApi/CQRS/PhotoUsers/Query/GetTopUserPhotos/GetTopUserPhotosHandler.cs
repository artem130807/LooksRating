using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTopUserPhotos
{
    public sealed class GetTopUserPhotosHandler
        : IRequestHandler<GetTopUserPhotosQuery, Result<GetTopUserPhotosPagedResponse>>
    {
        private readonly IGetTopUserPhotosValidator _validator;
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly IPhotoTopReadService _photoTopReadService;

        public GetTopUserPhotosHandler(
            IGetTopUserPhotosValidator validator,
            IPhotoUserRepository photoUserRepository,
            IPhotoTopReadService photoTopReadService)
        {
            _validator = validator;
            _photoUserRepository = photoUserRepository;
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
            var (photoIds, total) = await _photoTopReadService.GetTopPhotoIdsAsync(
                context.Season.Id,
                context.Season.IsClosed,
                context.FeedCity,
                context.Query.Gender,
                context.Query.Age,
                context.Skip,
                context.PageSize,
                cancellationToken);

            var photos = await _photoUserRepository.GetByIdsAsync(photoIds, cancellationToken);
            var photosById = photos.ToDictionary(p => p.Id);

            var items = new List<GetTopUserPhotosResponse>(photoIds.Count);
            foreach (var photoId in photoIds)
            {
                if (!photosById.TryGetValue(photoId, out var photo))
                {
                    continue;
                }

                items.Add(new GetTopUserPhotosResponse
                {
                    Id = photo.Id,
                    Place = context.Skip + items.Count + 1,
                    Name = UserPublicDisplayName.Resolve(photo.User),
                    TelegramFileId = photo.TelegramFileId,
                    Rating = photo.Rating,
                    RatingCount = photo.RatingCount,
                    GenderNomination = GenderDisplay.GetGender(photo.GenderNomination),
                    AgeNomination = photo.AgeNomination,
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
