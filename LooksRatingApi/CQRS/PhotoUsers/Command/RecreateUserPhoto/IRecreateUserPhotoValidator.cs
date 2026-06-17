using CSharpFunctionalExtensions;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.RecreateUserPhoto
{
    public interface IRecreateUserPhotoValidator
    {
        Task<Result<string>> ValidateAsync(RecreateUserPhotoCommand command, CancellationToken cancellationToken);
    }
}
