using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.Producers;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Messages.Kafka.SendUserReviewers;

public sealed class CreateReviewEventPublisherTests
{
    private readonly InMemoryReviewSequenceStore _store = new();
    private readonly CreateReviewEventPublisher _publisher;
    private readonly ICreateReviewEventProducer _producer = Substitute.For<ICreateReviewEventProducer>();

    public CreateReviewEventPublisherTests()
    {
        _publisher = new CreateReviewEventPublisher(
            new ReviewSequenceService(_store, new ReviewSequenceCalculator()),
            _producer,
            NullLogger<CreateReviewEventPublisher>.Instance);
    }

    [Fact]
    public async Task PublishAsync_FirstReviewOnProfile_PublishesCountOne()
    {
        var reviewerId = Guid.NewGuid();
        var photoProfileId = Guid.NewGuid();

        var result = await _publisher.PublishAsync(reviewerId, photoProfileId, CancellationToken.None);

        result.ReviewsCount.Should().Be(1);
        result.ReviewerId.Should().Be(reviewerId);
        result.PhotoProfileId.Should().Be(photoProfileId);
        result.IsNewReview.Should().BeTrue();

        await _producer.Received(1).ProduceAsync(
            Arg.Is<CreateReviewEvent>(e =>
                e.ReviewerId == reviewerId
                && e.PhotoProfileId == photoProfileId
                && e.ReviewsCount == 1
                && e.IsNewReview),
            CancellationToken.None);
    }

    [Fact]
    public async Task PublishAsync_SecondReviewerOnSameProfile_IncrementsProfileCount()
    {
        var photoProfileId = Guid.NewGuid();
        var key = new ReviewSequenceKey(photoProfileId);
        _store.SetLastReviewsCount(key, 4);

        var result = await _publisher.PublishAsync(Guid.NewGuid(), photoProfileId, CancellationToken.None);

        result.ReviewsCount.Should().Be(5);
        _store.GetLastReviewsCount(key).Should().Be(5);
    }

    [Fact]
    public async Task PublishAsync_TenthReviewerOnProfile_PublishesCountTen()
    {
        var photoProfileId = Guid.NewGuid();
        var key = new ReviewSequenceKey(photoProfileId);
        _store.SetLastReviewsCount(key, 9);

        var result = await _publisher.PublishAsync(Guid.NewGuid(), photoProfileId, CancellationToken.None);

        result.ReviewsCount.Should().Be(10);
    }

    [Fact]
    public async Task PublishAsync_AfterTenReviewsOnProfile_ResetsCountToOne()
    {
        var photoProfileId = Guid.NewGuid();
        var key = new ReviewSequenceKey(photoProfileId);
        _store.SetLastReviewsCount(key, 10);

        var result = await _publisher.PublishAsync(Guid.NewGuid(), photoProfileId, CancellationToken.None);

        result.ReviewsCount.Should().Be(1);
    }
}
