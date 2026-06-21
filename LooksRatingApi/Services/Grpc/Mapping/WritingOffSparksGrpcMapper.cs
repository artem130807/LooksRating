using LooksRatingGrpc;
using WritingOffSparksEntity = LooksRatingApi.Models.WritingOffSparks;

namespace LooksRatingApi.Services.Grpc.Mapping;

internal static class WritingOffSparksGrpcMapper
{
    public static WritingOffSparksItem ToItem(WritingOffSparksEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity.User);

        return new WritingOffSparksItem
        {
            Id = entity.Id.ToString(),
            Status = OutputStatusEnumMapper.ToGrpc(entity.Status),
            UserId = entity.UserId.ToString(),
            TelegramId = entity.User.TelegramId,
            City = entity.City,
            SparksCount = decimal.ToInt32(entity.SparksCount),
            Stars = entity.Stars,
            CreatedAtUnixSeconds = new DateTimeOffset(
                DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc)).ToUnixTimeSeconds(),
        };
    }
}
