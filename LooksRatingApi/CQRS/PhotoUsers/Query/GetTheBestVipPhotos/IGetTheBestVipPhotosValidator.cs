using CSharpFunctionalExtensions;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestVipPhotos
{
    public interface IGetTheBestVipPhotosValidator
    {
        Task<Result<GetTheBestVipPhotosValidatedContext>> ValidateAsync(
            GetTheBestVipPhotosQuery query,
            CancellationToken cancellationToken);
    }
}
