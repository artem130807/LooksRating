using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers;

namespace LooksRatingApi.Messages.Kafka.PhotoRated.Consumers
{
    public class KafkaValueDeserializer<TMessage>:IDeserializer<TMessage>
    {
        public TMessage Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (typeof(TMessage) == typeof(string))
            {
                var json = Encoding.UTF8.GetString(data);
                return (TMessage)(object)json; 
            }
            return JsonSerializer.Deserialize<TMessage>(data, KafkaJsonOptions.Value)!;
        }
    }
}