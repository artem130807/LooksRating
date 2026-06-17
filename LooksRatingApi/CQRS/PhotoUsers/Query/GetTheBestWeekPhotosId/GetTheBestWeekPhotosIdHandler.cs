using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosId;
using LooksRatingApi.Models;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query
{
    public class GetTheBestWeekPhotosIdHandler : IRequestHandler<GetTheBestWeekPhotosIdQuery, Result<List<long>>>
    {
        private readonly ITheBestWeekRepository _theBestWeekRepository;
        private readonly LooksRatingDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GetTheBestWeekPhotosIdHandler> _logger;
        public GetTheBestWeekPhotosIdHandler(ITheBestWeekRepository theBestWeekRepository, LooksRatingDbContext context, ILogger<GetTheBestWeekPhotosIdHandler> logger, IUserRepository userRepository)
        {
            _theBestWeekRepository = theBestWeekRepository;
            _context = context;
            _logger = logger;
            _userRepository = userRepository;
        }
        public async Task<Result<List<long>>> Handle(GetTheBestWeekPhotosIdQuery request, CancellationToken cancellationToken)
        {
            var ids = await _theBestWeekRepository.GetIds();
            if(ids.Count == 0)
                return Result.Failure<List<long>>("Список айди пуст");
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _userRepository.AddCountInTop(ids);
                await transaction.CommitAsync();
            }catch(Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex.Message);
            }
            return Result.Success(ids);
        }
    }
}