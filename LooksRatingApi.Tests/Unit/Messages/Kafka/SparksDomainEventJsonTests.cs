using System.Text.Json;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers;

namespace LooksRatingApi.Tests.Unit.Messages.Kafka;

public sealed class SparksDomainEventJsonTests
{
    [Fact]
    public void CurrencySparksEvent_RoundTripsThroughKafkaJson()
    {
        var ledgerId = Guid.NewGuid();
        var original = new CurrencySparksEvent(ledgerId, 1500m);

        var json = JsonSerializer.SerializeToUtf8Bytes(original, KafkaJsonOptions.Value);
        var restored = JsonSerializer.Deserialize<CurrencySparksEvent>(json, KafkaJsonOptions.Value);

        restored.Should().NotBeNull();
        restored!.AggregateId.Should().Be(ledgerId);
        restored.SparksCount.Should().Be(1500m);
    }

    [Fact]
    public void CurrencyDebitedEvent_RoundTripsThroughKafkaJson()
    {
        var ledgerId = Guid.NewGuid();
        var original = new CurrencyDebitedEvent(ledgerId, 42m);

        var json = JsonSerializer.SerializeToUtf8Bytes(original, KafkaJsonOptions.Value);
        var restored = JsonSerializer.Deserialize<CurrencyDebitedEvent>(json, KafkaJsonOptions.Value);

        restored.Should().NotBeNull();
        restored!.AggregateId.Should().Be(ledgerId);
        restored.SparksCount.Should().Be(42m);
    }

    [Fact]
    public void CurrencyDebitCompensatedEvent_RoundTripsThroughKafkaJson()
    {
        var ledgerId = Guid.NewGuid();
        var originalEventId = Guid.NewGuid();
        var original = new CurrencyDebitCompensatedEvent(
            ledgerId,
            100m,
            25m,
            originalEventId,
            "rollback");

        var json = JsonSerializer.SerializeToUtf8Bytes(original, KafkaJsonOptions.Value);
        var restored = JsonSerializer.Deserialize<CurrencyDebitCompensatedEvent>(json, KafkaJsonOptions.Value);

        restored.Should().NotBeNull();
        restored!.AggregateId.Should().Be(ledgerId);
        restored.NewSparksCount.Should().Be(100m);
        restored.CompensatedAmount.Should().Be(25m);
        restored.OriginalEventId.Should().Be(originalEventId);
        restored.CompensationReason.Should().Be("rollback");
    }
}
