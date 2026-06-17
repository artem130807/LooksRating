using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.UserContracts;
using MediatR;

namespace LooksRatingApi.CQRS.UserReferenceLink.Command.CreateUserReferenceLink
{
    public sealed class CreateUserReferenceLinkHandler : IRequestHandler<CreateUserReferenceLinkCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserReferenceLinkRepository _userReferenceLinkRepository;

        public CreateUserReferenceLinkHandler(
            IUserRepository userRepository,
            IUserReferenceLinkRepository userReferenceLinkRepository)
        {
            _userRepository = userRepository;
            _userReferenceLinkRepository = userReferenceLinkRepository;
        }

        public async Task<Result<string>> Handle(
            CreateUserReferenceLinkCommand command,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByTelegramId(command.telegramId);
            if (user is null)
            {
                return Result.Failure<string>("Пользователь не найден");
            }

            var link = await _userReferenceLinkRepository.EnsureLinkExistsAsync(user.Id, cancellationToken);
            return link.Link;
        }
    }
}
