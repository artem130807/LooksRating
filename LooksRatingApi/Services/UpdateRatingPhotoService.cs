using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Domain.DomainEvents;

namespace LooksRatingApi.Services
{
    public class UpdateRatingPhotoService : IUpdateRatingPhotoService
    {
        private readonly IPhotoRatingCacheService _photoRatingCacheService;

        public UpdateRatingPhotoService(IPhotoRatingCacheService photoRatingCacheService)
        {
            _photoRatingCacheService = photoRatingCacheService;
        }

        public async Task<Result> Update(List<PhotoRatedEvent> message, CancellationToken cancellationToken)
        {
            foreach (var photoRated in message)
            {
                await _photoRatingCacheService.SyncPhotoRatingAsync(photoRated, cancellationToken);
            }

            return Result.Success();
        }
    }
}
