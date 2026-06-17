using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Services.UserReferenceLinkService
{
    public static class GenerateReferenceLink
    {
        private static string link = "https://t.me/LooksRatingBot?start=";
        public static string GenerateLink(Guid UserId)
        {
            string userId = UserId.ToString();
            string fullLink = link + userId;
            return fullLink;
        }
    }
}