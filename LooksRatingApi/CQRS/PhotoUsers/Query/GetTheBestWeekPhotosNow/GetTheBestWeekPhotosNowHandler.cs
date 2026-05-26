using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosNow
{
    public sealed class GetTheBestWeekPhotosNowHandler
        : IRequestHandler<GetTheBestWeekPhotosNowQuery, Result<List<GetTheBestWeekPhotosNowResponse>>>
    {
        private readonly IGetTheBestWeekPhotosNowValidator _validator;
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly IPhotoTopReadService _photoTopReadService;

        public GetTheBestWeekPhotosNowHandler(
            IGetTheBestWeekPhotosNowValidator validator,
            IPhotoUserRepository photoUserRepository,
            IPhotoTopReadService photoTopReadService)
        {
            _validator = validator;
            _photoUserRepository = photoUserRepository;
            _photoTopReadService = photoTopReadService;
        }

        public async Task<Result<List<GetTheBestWeekPhotosNowResponse>>> Handle(
            GetTheBestWeekPhotosNowQuery query,
            CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(query, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<List<GetTheBestWeekPhotosNowResponse>>(validationResult.Error);
            }

            var context = validationResult.Value;
            var (photoIds, _) = await _photoTopReadService.GetTopPhotoIdsAsync(
                context.CurrentSeason.Id,
                context.CurrentSeason.IsClosed,
                context.FeedCity,
                context.Query.Gender,
                context.Query.Age,
                skip: 0,
                take: GetTheBestWeekPhotosNowValidatedContext.TopPhotoCount,
                cancellationToken);

            var photos = await _photoUserRepository.GetByIdsAsync(photoIds, cancellationToken);
            var photosById = photos.ToDictionary(p => p.Id);

            var items = new List<GetTheBestWeekPhotosNowResponse>(photoIds.Count);
            foreach (var photoId in photoIds)
            {
                if (!photosById.TryGetValue(photoId, out var photo))
                {
                    continue;
                }

                items.Add(new GetTheBestWeekPhotosNowResponse
                {
                    Id = photo.Id,
                    Place = items.Count + 1,
                    Name = UserPublicDisplayName.Resolve(photo.User),
                    TelegramFileId = photo.TelegramFileId,
                    Rating = photo.Rating,
                    RatingCount = photo.RatingCount,
                    GenderNomination = GenderDisplay.GetGender(photo.GenderNomination),
                    AgeNomination = photo.AgeNomination,
                });
            }

            return Result.Success(items);
        }
    }
}
