using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Services
{
    public class RankService : IRankService
    {
        public RankEnum GetRankEnum(decimal rating)
        {
            return rating switch
            {
                <= 2 => RankEnum.Terrible,
                <= 4 => RankEnum.Unattractive,
                <= 6 => RankEnum.Average,
                7 => RankEnum.Cute,
                8 or 9 => RankEnum.Beautiful,
                10 => RankEnum.Gorgeous,
                _ => RankEnum.Average
            };
        }
    }
}