using CSharpFunctionalExtensions;

namespace LooksRatingApi.CQRS.Users.Command.UpdateGenderUser
{
    public interface IUpdateGenderUserValidator
    {
        Task<Result<string>> ValidateAsync(UpdateGenderUserCommandCommand command, CancellationToken cancellationToken);
    }
}
