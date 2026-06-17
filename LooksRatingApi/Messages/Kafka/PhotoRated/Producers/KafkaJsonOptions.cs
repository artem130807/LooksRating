using System.Text.Json;

namespace LooksRatingApi.Messages.Kafka.PhotoRated.Producers
{
    public static class KafkaJsonOptions
    {
        internal static readonly JsonSerializerOptions Value = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
    }
}
