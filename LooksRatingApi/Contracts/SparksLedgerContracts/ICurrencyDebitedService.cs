using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;

namespace LooksRatingApi.Contracts.SparksLedgerContracts
{
    public interface ICurrencyDebitedService
    {
        Task<Guid> Debited(Guid userId, decimal debitedSparks, CancellationToken cancellationToken);
    }
}