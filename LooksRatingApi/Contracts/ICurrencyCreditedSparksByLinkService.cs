using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace LooksRatingApi.Contracts
{
    public interface ICurrencyCreditedSparksByLinkService
    {
        Task Currency(string? Id);
    }
}