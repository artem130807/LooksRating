using FluentAssertions;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services.SparksWallet;

namespace LooksRatingApi.Tests.Unit.Services.SparksWallet;

public sealed class SparksDebitOrphanPolicyTests
{
    private const string KeyA = "writing-off-sparks:1:key-a";
    private const string KeyB = "writing-off-sparks:1:key-b";

    [Fact]
    public void GetResolution_ReturnsNone_WhenDebitAwaitingCreateOnSameKey()
    {
        var idempotency = CreateIdempotency(KeyA);

        var resolution = SparksDebitOrphanPolicy.GetResolution(idempotency, writingOff: null, KeyA);

        resolution.Should().Be(SparksDebitOrphanPolicy.OrphanResolution.None);
    }

    [Fact]
    public void GetResolution_ReturnsCompensateAndMark_WhenDebitHasNoWritingOff()
    {
        var idempotency = CreateIdempotency(KeyA);

        var resolution = SparksDebitOrphanPolicy.GetResolution(idempotency, writingOff: null, KeyB);

        resolution.Should().Be(SparksDebitOrphanPolicy.OrphanResolution.CompensateAndMark);
    }

    [Fact]
    public void GetResolution_ReturnsNone_WhenPendingWritingOffExists()
    {
        var idempotency = CreateIdempotency(KeyA);
        var writingOff = CreateWritingOff(KeyA, OutputStatusEnum.Pending);

        var resolution = SparksDebitOrphanPolicy.GetResolution(idempotency, writingOff, KeyB);

        resolution.Should().Be(SparksDebitOrphanPolicy.OrphanResolution.None);
    }

    [Fact]
    public void GetResolution_ReturnsMarkOnly_WhenWritingOffCancelled()
    {
        var idempotency = CreateIdempotency(KeyA);
        var writingOff = CreateWritingOff(KeyA, OutputStatusEnum.Cancelled);

        var resolution = SparksDebitOrphanPolicy.GetResolution(idempotency, writingOff, KeyB);

        resolution.Should().Be(SparksDebitOrphanPolicy.OrphanResolution.MarkOnly);
    }

    [Fact]
    public void GetResolution_ReturnsNone_WhenAlreadyCompensated()
    {
        var idempotency = CreateIdempotency(KeyA);
        idempotency.MarkCompensated();

        var resolution = SparksDebitOrphanPolicy.GetResolution(idempotency, writingOff: null, KeyB);

        resolution.Should().Be(SparksDebitOrphanPolicy.OrphanResolution.None);
    }

    private static SparksDebitIdempotency CreateIdempotency(string key) =>
        SparksDebitIdempotency.Create(Guid.NewGuid(), key, Guid.NewGuid(), 1200m, 100).Value;

    private static Models.WritingOffSparks CreateWritingOff(string key, OutputStatusEnum status)
    {
        var writingOff = Models.WritingOffSparks.Create(Guid.NewGuid(), 1200m, key, 100, "moscow").Value;
        writingOff.UpdateStatus(status);
        return writingOff;
    }
}
