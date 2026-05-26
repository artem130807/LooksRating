using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Contracts
{
    public interface IRankService
    {
        RankEnum GetRankEnum(decimal rating);
    }
}