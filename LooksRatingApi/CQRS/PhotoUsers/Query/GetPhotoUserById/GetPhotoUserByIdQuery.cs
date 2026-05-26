using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetPhotoUserById
{
    public record GetPhotoUserByIdQuery(Guid Id):IRequest<Result<GetPhotoUserByIdResponse>>;
}