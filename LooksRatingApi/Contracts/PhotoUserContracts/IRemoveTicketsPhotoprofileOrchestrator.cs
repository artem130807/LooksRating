using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingGrpc;

namespace LooksRatingApi.Contracts.PhotoUserContracts
{
    public interface IRemoveTicketsPhotoprofileOrchestrator
    {
        Task<Result<RemoveTicketsPhotoprofileResponse>> RemoveTickets(Guid photoProfileId, CancellationToken cancellationToken);
    }
}