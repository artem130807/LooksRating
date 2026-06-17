using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.UserReferenceLink.Command.CreateUserReferenceLink
{
    public record CreateUserReferenceLinkCommand(long telegramId):IRequest<Result<string>>;
}