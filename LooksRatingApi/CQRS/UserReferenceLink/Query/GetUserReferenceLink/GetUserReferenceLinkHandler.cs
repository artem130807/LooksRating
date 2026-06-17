using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.UserContracts;
using MediatR;
using Microsoft.IdentityModel.Tokens;

namespace LooksRatingApi.CQRS.UserReferenceLink.Query.GetUserReferenceLink
{
    public class GetUserReferenceLinkHandler : IRequestHandler<GetUserReferenceLinkQuery, Result<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserReferenceLinkRepository _userReferenceLinkRepository;
        public GetUserReferenceLinkHandler(IUserRepository userRepository, IUserReferenceLinkRepository userReferenceLinkRepository)
        {
            _userRepository = userRepository;
            _userReferenceLinkRepository = userReferenceLinkRepository;
        }
        public async Task<Result<string>> Handle(GetUserReferenceLinkQuery query, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByTelegramId(query.telegramId);
            if(user == null)
                return Result.Failure<string>("Пользователь не найден");
            var userReference = await _userReferenceLinkRepository.GetByUserId(user.Id);
            if(userReference == null)
                return Result.Failure<string>("Сссылка не найдена");
            return userReference.Link;
        }
    }
}