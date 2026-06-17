using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.UserTicketContracts;
using LooksRatingGrpc;

namespace LooksRatingApi.Services.Orchestrators
{
    public class RejectTicketPhotoProfileOrchestrator : IRejectTicketPhotoProfileOrchestrator
    {
        private readonly IUserTicketRepository _userTicketRepository;
        private readonly LooksRatingDbContext _context;
        private readonly ILogger<RejectTicketPhotoProfileOrchestrator> _logger;
        public RejectTicketPhotoProfileOrchestrator(IUserTicketRepository userTicketRepository, LooksRatingDbContext context, ILogger<RejectTicketPhotoProfileOrchestrator> logger)
        {
            _userTicketRepository = userTicketRepository;
            _context = context;
            _logger = logger;
        }
        public async Task<Result<RejectTicketPhotoProfileResponse>> RejectTicket(Guid ticketId, CancellationToken cancellationToken)
        {
            var userTicket = await _userTicketRepository.GetTicketById(ticketId);
            if(userTicket == null)
                return Result.Success(new RejectTicketPhotoProfileResponse{Message = "Тикен не найден", IsSuccess = false});
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _userTicketRepository.Delete(ticketId);
                await transaction.CommitAsync();
            }catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                await transaction.RollbackAsync();
            }
            return Result.Success(new RejectTicketPhotoProfileResponse{Message = "Успешно", IsSuccess = true});
        }
    }
}