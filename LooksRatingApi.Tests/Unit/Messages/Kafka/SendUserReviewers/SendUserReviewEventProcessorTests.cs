using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.Processing;
using LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Messages.Kafka.SendUserReviewers;

public sealed class SendUserReviewEventProcessorTests
{
    private readonly InMemoryReviewSequenceStore _store = new();
    private readonly IReviewMilestoneNotifier _notifier = Substitute.For<IReviewMilestoneNotifier>();
    private readonly SendUserReviewEventProcessor _processor;

    public SendUserReviewEventProcessorTests()
    {
        _processor = new SendUserReviewEventProcessor(
            _store,
            new ReviewSequenceService(_store, new ReviewSequenceCalculator()),
            _notifier,
            NullLogger<SendUserReviewEventProcessor>.Instance);
    }

    [Fact]
    public async Task ProcessAsync_FirstReview_SetsCountToOne()
    {
        var reviewerId = Guid.NewGuid();
        var photoProfileId = Guid.NewGuid();
        var incoming = new CreateReviewEvent(reviewerId, photoProfileId);

        var result = await _processor.ProcessAsync(incoming, CancellationToken.None);

        result.ReviewsCount.Should().Be(1);
        result.ReviewerId.Should().Be(reviewerId);
        _store.GetLastReviewsCount(new ReviewSequenceKey(photoProfileId)).Should().Be(1);
        await _notifier.DidNotReceive().TryNotifyAsync(Arg.Any<CreateReviewEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_AfterTenReviews_ResetsCountToOne()
    {
        var reviewerId = Guid.NewGuid();
        var photoProfileId = Guid.NewGuid();
        var key = new ReviewSequenceKey(photoProfileId);
        _store.SetLastReviewsCount(key, 10);

        var result = await _processor.ProcessAsync(
            new CreateReviewEvent(reviewerId, photoProfileId),
            CancellationToken.None);

        result.ReviewsCount.Should().Be(1);
        _store.GetLastReviewsCount(key).Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_IncrementsCountWithinCycle()
    {
        var reviewerId = Guid.NewGuid();
        var photoProfileId = Guid.NewGuid();
        var key = new ReviewSequenceKey(photoProfileId);
        _store.SetLastReviewsCount(key, 4);

        var result = await _processor.ProcessAsync(
            new CreateReviewEvent(reviewerId, photoProfileId),
            CancellationToken.None);

        result.ReviewsCount.Should().Be(5);
    }

    [Fact]
    public async Task ProcessAsync_WhenAlreadyEnriched_DoesNotIncrementAgain()
    {
        var reviewerId = Guid.NewGuid();
        var photoProfileId = Guid.NewGuid();
        var key = new ReviewSequenceKey(photoProfileId);
        _store.SetLastReviewsCount(key, 4);

        var result = await _processor.ProcessAsync(
            new CreateReviewEvent(reviewerId, photoProfileId, reviewsCount: 7),
            CancellationToken.None);

        result.ReviewsCount.Should().Be(7);
        _store.GetLastReviewsCount(key).Should().Be(7);
    }

    [Fact]
    public async Task ProcessAsync_WhenCountIsTen_CallsMilestoneNotifier()
    {
        var reviewerId = Guid.NewGuid();
        var photoProfileId = Guid.NewGuid();
        var incoming = new CreateReviewEvent(reviewerId, photoProfileId, reviewsCount: 10, isNewReview: true);

        var result = await _processor.ProcessAsync(incoming, CancellationToken.None);

        result.ReviewsCount.Should().Be(10);
        await _notifier.Received(1).TryNotifyAsync(
            Arg.Is<CreateReviewEvent>(e => e.ReviewsCount == 10 && e.PhotoProfileId == photoProfileId),
            CancellationToken.None);
    }
}
