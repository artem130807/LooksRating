using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.UserReferenceLink.Query.GetUserReferenceLink
{
    public record GetUserReferenceLinkQuery(long telegramId):IRequest<Result<string>>;
}