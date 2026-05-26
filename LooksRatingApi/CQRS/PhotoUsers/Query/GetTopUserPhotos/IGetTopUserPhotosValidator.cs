using CSharpFunctionalExtensions;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTopUserPhotos
{
    public interface IGetTopUserPhotosValidator
    {
        Task<Result<GetTopUserPhotosValidatedContext>> ValidateAsync(
            GetTopUserPhotosQuery query,
            CancellationToken cancellationToken);
    }
}
