using CSharpFunctionalExtensions;

namespace LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto
{
    public interface ISetUserPhotoValidator
    {
        Task<Result<string>> ValidateAsync(SetUserPhotoCommand command, CancellationToken cancellationToken);
    }
}
