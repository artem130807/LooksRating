namespace LooksRatingApi.Infrastructure.Kafka
{
    public static class KafkaFeature
    {
        public static bool IsEnabled(IConfiguration configuration)
        {
            if (!configuration.GetValue("Kafka:Enabled", true))
                return false;

            var bootstrapServers = configuration["Kafka:BootstrapServers"];
            return !string.IsNullOrWhiteSpace(bootstrapServers);
        }
    }
}
