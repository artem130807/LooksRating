using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.UserReferenceLink;
using MediatR;

namespace LooksRatingApi.CQRS.UserReferenceLink.Command.CreateUserReferenceLink
{
    public sealed class CreateUserReferenceLinkHandler
        : IRequestHandler<CreateUserReferenceLinkCommand, Result<UserReferenceLinkResponse>>
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

        public async Task<Result<UserReferenceLinkResponse>> Handle(
            CreateUserReferenceLinkCommand command,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByTelegramId(command.telegramId);
            if (user is null)
            {
                return Result.Failure<UserReferenceLinkResponse>("Пользователь не найден");
            }

            var link = await _userReferenceLinkRepository.EnsureLinkExistsAsync(user.Id, cancellationToken);
            return UserReferenceLinkResponse.FromModel(link);
        }
    }
}
