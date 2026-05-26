using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosId;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query
{
    public class GetTheBestWeekPhotosIdHandler : IRequestHandler<GetTheBestWeekPhotosIdQuery, Result<List<long>>>
    {
        private readonly ITheBestWeekRepository _theBestWeekRepository;
        public GetTheBestWeekPhotosIdHandler(ITheBestWeekRepository theBestWeekRepository)
        {
            _theBestWeekRepository = theBestWeekRepository;
        }
        public async Task<Result<List<long>>> Handle(GetTheBestWeekPhotosIdQuery request, CancellationToken cancellationToken)
        {
            var ids = await _theBestWeekRepository.GetIds();
            if(ids.Count == 0)
                return Result.Failure<List<long>>("Список айди пуст");
            return Result.Success(ids);
        }
    }
}