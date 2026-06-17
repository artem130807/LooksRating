using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Messages.Kafka.PhotoRated.Producers
{
    public class KafkaProducerSettings
    {
        public string BootstrapServers {get ; set;}
        public  Dictionary<string, string> Topics {get; set;} = new();
    }
}