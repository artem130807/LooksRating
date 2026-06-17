using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LooksRatingApi.Messages.Kafka.PhotoRated.Consumers
{
    public class KafkaConsumerSettings
    {
        public string BootstrapServers {get ; set;}
        public string Topic {get; set;}
        public string GroupId {get; set;}
    }
}