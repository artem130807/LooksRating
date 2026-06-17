using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Services
{
    public class RankDisplay
    {
        private static readonly Dictionary<RankEnum, string> _stickers = new()
        {
            { RankEnum.Terrible, "🤮" },
            { RankEnum.Unattractive, "😞" },
            { RankEnum.Average, "😶" },
            { RankEnum.Cute, "🙂" },
            { RankEnum.Beautiful, "😊" },
            { RankEnum.Gorgeous, "🤩" }
        };

    public static string GetSticker(RankEnum rank) => _stickers[rank];
    }
}