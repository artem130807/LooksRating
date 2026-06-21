using FluentAssertions;
using LooksRatingApi.Enums;
using LooksRatingApi.Services.Grpc.Mapping;
using GrpcOutputStatusEnum = LooksRatingGrpc.OutputStatusEnum;

namespace LooksRatingApi.Tests.Unit.Services.WritingOffSparks;

public sealed class OutputStatusEnumMapperTests
{
    [Theory]
    [InlineData(GrpcOutputStatusEnum.Pending, OutputStatusEnum.Pending)]
    [InlineData(GrpcOutputStatusEnum.Cancelled, OutputStatusEnum.Cancelled)]
    [InlineData(GrpcOutputStatusEnum.Confirmed, OutputStatusEnum.Confirmed)]
    [InlineData(GrpcOutputStatusEnum.Failed, OutputStatusEnum.Failed)]
    public void TryToDomain_MapsKnownStatuses(
        GrpcOutputStatusEnum grpcStatus,
        OutputStatusEnum expected)
    {
        var mapped = OutputStatusEnumMapper.TryToDomain(grpcStatus, out var domainStatus);

        mapped.Should().BeTrue();
        domainStatus.Should().Be(expected);
    }

    [Fact]
    public void TryToDomain_RejectsUnspecifiedStatus()
    {
        var mapped = OutputStatusEnumMapper.TryToDomain(
            GrpcOutputStatusEnum.Unspecified,
            out _);

        mapped.Should().BeFalse();
    }
}
