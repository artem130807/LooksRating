using CSharpFunctionalExtensions;

namespace LooksRatingApi.CQRS.Users.Command.UpdateUserAge
{
    public interface IUpdateUserAgeValidator
    {
        Task<Result<string>> ValidateAsync(UpdateUserAgeCommand command, CancellationToken cancellationToken);
    }
}
