using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.UserContracts;
using MediatR;

namespace LooksRatingApi.CQRS.UserReferenceLink.Command.CreateUserReferenceLink
{
    public class CreateUserReferenceLinkHandler : IRequestHandler<CreateUserReferenceLinkCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserReferenceLinkRepository _userReferenceLinkRepository;
        public CreateUserReferenceLinkHandler(IUserRepository userRepository, IUserReferenceLinkRepository userReferenceLinkRepository)
        {
            _userRepository = userRepository;
            _userReferenceLinkRepository = userReferenceLinkRepository;
        }
        public async Task<Result<string>> Handle(CreateUserReferenceLinkCommand command, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByTelegramId(command.telegramId);
            if(user == null)
                return Result.Failure<string>("Пользователь не найден");
            var link = Models.UserReferenceLink.Create(user.Id);
            await _userReferenceLinkRepository.Add(link.Value);
            await _userReferenceLinkRepository.SaveChangesAsync();
            return link.Value.Link;
        }
    }
}