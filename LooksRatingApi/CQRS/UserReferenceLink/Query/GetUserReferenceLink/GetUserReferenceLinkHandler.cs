using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.UserReferenceLink;
using MediatR;

namespace LooksRatingApi.CQRS.UserReferenceLink.Query.GetUserReferenceLink
{
    public sealed class GetUserReferenceLinkHandler
        : IRequestHandler<GetUserReferenceLinkQuery, Result<UserReferenceLinkResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserReferenceLinkRepository _userReferenceLinkRepository;

        public GetUserReferenceLinkHandler(
            IUserRepository userRepository,
            IUserReferenceLinkRepository userReferenceLinkRepository)
        {
            _userRepository = userRepository;
            _userReferenceLinkRepository = userReferenceLinkRepository;
        }

        public async Task<Result<UserReferenceLinkResponse>> Handle(
            GetUserReferenceLinkQuery query,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByTelegramId(query.telegramId);
            if (user is null)
            {
                return Result.Failure<UserReferenceLinkResponse>("Пользователь не найден");
            }

            var userReference = await _userReferenceLinkRepository.GetByUserIdAsync(user.Id, cancellationToken);
            if (userReference is null)
            {
                return Result.Failure<UserReferenceLinkResponse>("Ссылка не найдена");
            }

            return UserReferenceLinkResponse.FromModel(userReference);
        }
    }
}
