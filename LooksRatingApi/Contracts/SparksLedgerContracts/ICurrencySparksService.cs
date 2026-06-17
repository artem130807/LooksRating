using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace LooksRatingApi.Contracts.SparksLedgerContracts
{
    public interface ICurrencySparksService
    {
        Task Credited(Guid userId, decimal debitedSparks, CancellationToken cancellationToken);
    }
}