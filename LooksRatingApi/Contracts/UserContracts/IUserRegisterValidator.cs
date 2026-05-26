using CSharpFunctionalExtensions;

namespace LooksRatingApi.Cqrs.Users.Command.RegisterUser
{
    public interface IUserRegisterValidator
    {
        Task<Result<string>> ValidateAsync(RegisterUserCommand command, CancellationToken cancellationToken);
    }
}
