using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;

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
            return JsonSerializer.Deserialize<TMessage>(data)!;
        }
    }
}