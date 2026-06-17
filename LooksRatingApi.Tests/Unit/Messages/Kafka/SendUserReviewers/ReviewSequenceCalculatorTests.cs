using LooksRatingApi.Messages.Kafka.SendUserReviewers.ReviewSequence;

namespace LooksRatingApi.Tests.Unit.Messages.Kafka.SendUserReviewers;

public sealed class ReviewSequenceCalculatorTests
{
    private readonly ReviewSequenceCalculator _calculator = new();

    [Theory]
    [InlineData(null, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(9, 10)]
    [InlineData(10, 1)]
    [InlineData(11, 1)]
    public void CalculateNextReviewsCount_FollowsCycleRules(int? previous, int expected)
    {
        _calculator.CalculateNextReviewsCount(previous).Should().Be(expected);
    }
}
