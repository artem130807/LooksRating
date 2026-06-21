using FluentAssertions;
using Grpc.Core;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Enums;
using LooksRatingApi.Services.GrpcService;
using LooksRatingGrpc;
using LooksRatingApi.Tests.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using DomainOutputStatusEnum = LooksRatingApi.Enums.OutputStatusEnum;

namespace LooksRatingApi.Tests.Unit.Services.WritingOffSparks;

public sealed class UpdateStatusWritingOffSparksGrpcServiceTests
{
    [Fact]
    public async Task UpdateStatusWritingOffSparks_ReturnsFailure_WhenIdIsInvalid()
    {
        var orchestrator = Substitute.For<IUpdateStatusWritingOffSparksOrchestrator>();
        var service = new UpdateStatusWritingOffSparksGrpcService(orchestrator, GrpcTestAuth.Disabled());

        var response = await service.UpdateStatusWritingOffSparks(
            new UpdateStatusWritingOffSparksRequest
            {
                WritingOffSparksId = "not-a-guid",
                Status = LooksRatingGrpc.OutputStatusEnum.Confirmed,
            },
            CreateContext());

        response.Success.Should().BeFalse();
        response.Message.Should().Be("Некорректный идентификатор списания искр");
        await orchestrator.DidNotReceiveWithAnyArgs().UpdateStatusAsync(
            default,
            default,
            default);
    }

    [Fact]
    public async Task UpdateStatusWritingOffSparks_ReturnsFailure_WhenStatusIsUnspecified()
    {
        var orchestrator = Substitute.For<IUpdateStatusWritingOffSparksOrchestrator>();
        var service = new UpdateStatusWritingOffSparksGrpcService(orchestrator, GrpcTestAuth.Disabled());

        var response = await service.UpdateStatusWritingOffSparks(
            new UpdateStatusWritingOffSparksRequest
            {
                WritingOffSparksId = Guid.NewGuid().ToString(),
                Status = LooksRatingGrpc.OutputStatusEnum.Unspecified,
            },
            CreateContext());

        response.Success.Should().BeFalse();
        response.Message.Should().Be("Некорректный статус списания искр");
        await orchestrator.DidNotReceiveWithAnyArgs().UpdateStatusAsync(
            default,
            default,
            default);
    }

    [Fact]
    public async Task UpdateStatusWritingOffSparks_DelegatesToOrchestrator_WhenRequestIsValid()
    {
        var writingOffSparksId = Guid.NewGuid();
        var orchestrator = Substitute.For<IUpdateStatusWritingOffSparksOrchestrator>();
        orchestrator
            .UpdateStatusAsync(
                writingOffSparksId,
                DomainOutputStatusEnum.Confirmed,
                Arg.Any<CancellationToken>())
            .Returns(CSharpFunctionalExtensions.Result.Success(new UpdateStatusWritingOffSparksResponse
            {
                Success = true,
                Message = "ok",
            }));

        var service = new UpdateStatusWritingOffSparksGrpcService(orchestrator, GrpcTestAuth.Disabled());

        var response = await service.UpdateStatusWritingOffSparks(
            new UpdateStatusWritingOffSparksRequest
            {
                WritingOffSparksId = writingOffSparksId.ToString(),
                Status = LooksRatingGrpc.OutputStatusEnum.Confirmed,
            },
            CreateContext());

        response.Success.Should().BeTrue();
        response.Message.Should().Be("ok");
        await orchestrator.Received(1).UpdateStatusAsync(
            writingOffSparksId,
            DomainOutputStatusEnum.Confirmed,
            Arg.Any<CancellationToken>());
    }

    private static ServerCallContext CreateContext() =>
        Substitute.For<ServerCallContext>();
}
