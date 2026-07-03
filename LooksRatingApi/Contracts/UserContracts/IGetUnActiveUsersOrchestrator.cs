using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using LooksRatingGrpc;

namespace LooksRatingApi.Contracts.UserContracts
{
    public interface IGetUnActiveUsersOrchestrator
    {
        Task<Result<GetUnActiveUsersResponse>> GetUsers(CancellationToken cancellationToken = default);
    }
}