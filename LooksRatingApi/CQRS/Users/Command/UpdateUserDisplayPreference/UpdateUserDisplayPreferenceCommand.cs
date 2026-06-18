using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Command.UpdateUserDisplayPreference
{
    public sealed record UpdateUserDisplayPreferenceCommand(
        long TelegramId,
        string? TelegramUsername,
        bool UseTelegramUsernameAsDisplay,
        string? CustomName) : IRequest<Result<UpdateUserDisplayPreferenceResult>>;
}
