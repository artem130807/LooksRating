using LooksRatingApi.Services;

namespace LooksRatingApi.Tests.Unit.Services;

public sealed class TopServiceTests
{
    [Theory]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(47)]
    [InlineData(67)]
    [InlineData(100)]
    public void GetTop_ForUnsupportedAge_ReturnsEmpty(int age)
    {
        TopService.GetTop(age).Should().BeEmpty();
    }

    [Theory]
    [InlineData(14, 14, 15, 16)]
    [InlineData(16, 14, 15, 16)]
    [InlineData(46, 44, 45, 46)]
    public void GetTop_ForSupportedAge_ReturnsBracket(int age, int a, int b, int c)
    {
        TopService.GetTop(age).Should().Equal(a, b, c);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(14, true)]
    [InlineData(46, true)]
    [InlineData(67, false)]
    [InlineData(11, false)]
    public void IsValidFeedAge_MatchesBracketRules(int age, bool expected)
    {
        TopService.IsValidFeedAge(age).Should().Be(expected);
    }

    [Theory]
    [InlineData(14, true)]
    [InlineData(46, true)]
    [InlineData(0, false)]
    [InlineData(67, false)]
    public void IsValidNominationAge_RequiresBracketAge(int age, bool expected)
    {
        TopService.IsValidNominationAge(age).Should().Be(expected);
    }

    [Fact]
    public void AgeBrackets_StartAt14_AndEndAt46()
    {
        var ages = TopService.GetIntsList()
            .SelectMany(bracket => bracket)
            .Distinct()
            .OrderBy(age => age)
            .ToArray();

        ages.Min().Should().Be(TopService.MinBracketAge);
        ages.Max().Should().Be(TopService.MaxBracketAge);
        ages.Should().NotContain(11);
        ages.Should().NotContain(13);
        ages.Should().NotContain(47);
    }
}
