namespace LooksRatingApi.Services.SparksWallet
{
    public static class ReferralLinkParser
    {
        private const string StartParameter = "start=";

        public static bool TryParseReferrerUserId(string? referralLink, out Guid referrerUserId)
        {
            referrerUserId = Guid.Empty;
            if (string.IsNullOrWhiteSpace(referralLink))
            {
                return false;
            }

            var value = referralLink.Trim();

            var startIndex = value.IndexOf(StartParameter, StringComparison.OrdinalIgnoreCase);
            if (startIndex >= 0)
            {
                value = value[(startIndex + StartParameter.Length)..];
                var ampersandIndex = value.IndexOf('&');
                if (ampersandIndex >= 0)
                {
                    value = value[..ampersandIndex];
                }
            }

            value = value.Trim();
            return Guid.TryParse(value, out referrerUserId) && referrerUserId != Guid.Empty;
        }
    }
}
