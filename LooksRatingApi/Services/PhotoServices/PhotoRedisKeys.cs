namespace LooksRatingApi.Services
{
    public static class PhotoRedisKeys
    {
        public static string RatingSortedSet(string normalizedCityKey, Guid seasonId) =>
            $"photos:by_rating:{normalizedCityKey}, season{seasonId}";

        public static string UserRatedSet(Guid userId) =>
            $"user:{userId}:rated";

        public static string CycleAnchor(Guid userId) =>
            $"user:{userId}:cycle_anchor";

        public static string PhotoHash(Guid photoId) =>
            $"photo:{photoId}";
    }
}
