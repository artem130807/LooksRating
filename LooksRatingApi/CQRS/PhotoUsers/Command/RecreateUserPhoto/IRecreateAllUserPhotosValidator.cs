using CSharpFunctionalExtensions;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.RecreateUserPhoto
{
    public interface IRecreateAllUserPhotosValidator
    {
        Task<Result<string>> ValidateAsync(
            RecreateAllUserPhotosCommand command,
            CancellationToken cancellationToken);
    }
}
