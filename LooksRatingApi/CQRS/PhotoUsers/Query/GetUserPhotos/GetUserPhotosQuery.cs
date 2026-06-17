using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Enums;
using LooksRatingApi.Filters;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetUserPhotos
{
    public record GetUserPhotosQuery(long telegramId):IRequest<Result<GetUserPhotosResponse>>;
}