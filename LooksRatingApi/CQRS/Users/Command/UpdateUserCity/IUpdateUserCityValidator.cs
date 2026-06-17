using CSharpFunctionalExtensions;

namespace LooksRatingApi.CQRS.Users.Command.UpdateUserCity
{
    public interface IUpdateUserCityValidator
    {
        Task<Result<string>> ValidateAsync(UpdateUserCityCommand command, CancellationToken cancellationToken);
    }
}
