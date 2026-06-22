namespace LooksRatingApi.Services.SeasonLifecycle
{
    internal static class SeasonRolloverEventId
    {
        public static string Create(Guid closedSeasonId, Guid newSeasonId) =>
            $"{closedSeasonId:N}:{newSeasonId:N}";

        public static bool TryParse(string eventId, out Guid closedSeasonId, out Guid newSeasonId)
        {
            closedSeasonId = Guid.Empty;
            newSeasonId = Guid.Empty;
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return false;
            }

            var parts = eventId.Split(':', 2, StringSplitOptions.TrimEntries);
            return parts.Length == 2
                && Guid.TryParse(parts[0], out closedSeasonId)
                && Guid.TryParse(parts[1], out newSeasonId);
        }
    }
}
