namespace LooksRatingApi.Services
{
    public static class PhotoRedisKeys
    {
        public static string RatingSortedSet(string normalizedCityKey, Guid seasonId) =>
            $"profiles:by_rating:{normalizedCityKey}, season:{seasonId}";

        public static string UserRatedSet(Guid userId, Guid seasonId) =>
            $"user:{userId}:season:{seasonId}:rated";

        public static string CycleAnchor(Guid userId, Guid seasonId) =>
            $"user:{userId}:season:{seasonId}:cycle_anchor";

        public static string FeedRatingCounter(Guid userId, Guid seasonId) =>
            $"user:{userId}:season:{seasonId}:feed_rating_counter";

        public static string SkipFeedRepair(Guid userId, Guid seasonId) =>
            $"user:{userId}:season:{seasonId}:skip_feed_repair";

        public static string UnviewableProfilesSet(Guid userId) =>
            $"user:{userId}:unviewable_profiles";

        public static string ProfileHash(Guid profileId) =>
            $"profile:{profileId}";

        public static string PhotoHash(Guid photoId) =>
            ProfileHash(photoId);

        public static string SeasonRolloverEventMeta(string eventId) =>
            $"season-rollover:event:{eventId}:meta";

        public static string SeasonRolloverEventPending(string eventId) =>
            $"season-rollover:event:{eventId}:pending";

        public static string SeasonRolloverActiveEvents() =>
            "season-rollover:active-events";
    }
}
