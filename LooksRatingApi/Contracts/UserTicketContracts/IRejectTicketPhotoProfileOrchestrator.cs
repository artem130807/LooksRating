using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingGrpc;

namespace LooksRatingApi.Contracts.UserTicketContracts
{
    public interface IRejectTicketPhotoProfileOrchestrator
    {
        Task<Result<RejectTicketPhotoProfileResponse>> RejectTicket(Guid ticketId, CancellationToken cancellationToken);
    }
}