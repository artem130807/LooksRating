using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosId
{
    public record GetTheBestWeekPhotosIdQuery():IRequest<Result<List<long>>>;
}