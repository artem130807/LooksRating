using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Contracts
{
    public interface IAddCountInTopService
    {
        Task Handle(List<long> ids);
    }
}