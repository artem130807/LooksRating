using LooksRatingApi.Infrastructure.Quartz;

namespace LooksRatingApi.Tests.Infrastructure.Fakes;

public sealed class FakeApplicationClock : ApplicationClock
{
    private DateTime _localNow;

    public FakeApplicationClock(DateTime localNow)
        : base(ApplicationTimeZoneResolver.Resolve(ApplicationTimeZoneResolver.DefaultTimeZoneId))
    {
        _localNow = localNow;
    }

    public void SetNow(DateTime localNow) => _localNow = localNow;

    public override DateTime GetNow() => _localNow;
}
