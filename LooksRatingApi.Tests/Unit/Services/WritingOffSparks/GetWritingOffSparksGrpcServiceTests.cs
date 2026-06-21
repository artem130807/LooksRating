using FluentAssertions;
using Grpc.Core;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Services.GrpcService;
using LooksRatingGrpc;
using LooksRatingApi.Tests.Infrastructure.Helpers;

namespace LooksRatingApi.Tests.Unit.Services.WritingOffSparks;

public sealed class GetWritingOffSparksGrpcServiceTests
{
    [Fact]
    public async Task GetWritingOffSparks_ReturnsFailure_WhenIdIsInvalid()
    {
        var orchestrator = Substitute.For<IGetWritingOffSparksOrchestrator>();
        var service = new GetWritingOffSparksGrpcService(orchestrator, GrpcTestAuth.Disabled());

        var response = await service.GetWritingOffSparks(
            new GetWritingOffSparksRequest { WritingOffSparksId = "bad-id" },
            CreateContext());

        response.Success.Should().BeFalse();
        response.Message.Should().Be("Некорректный идентификатор списания искр");
        await orchestrator.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task GetWritingOffSparks_DelegatesToOrchestrator_WhenIdIsValid()
    {
        var id = Guid.NewGuid();
        var orchestrator = Substitute.For<IGetWritingOffSparksOrchestrator>();
        orchestrator
            .GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(CSharpFunctionalExtensions.Result.Success(new GetWritingOffSparksResponse
            {
                Success = true,
                Message = "ok",
            }));

        var service = new GetWritingOffSparksGrpcService(orchestrator, GrpcTestAuth.Disabled());

        var response = await service.GetWritingOffSparks(
            new GetWritingOffSparksRequest { WritingOffSparksId = id.ToString() },
            CreateContext());

        response.Success.Should().BeTrue();
        await orchestrator.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
    }

    private static ServerCallContext CreateContext() => Substitute.For<ServerCallContext>();
}
