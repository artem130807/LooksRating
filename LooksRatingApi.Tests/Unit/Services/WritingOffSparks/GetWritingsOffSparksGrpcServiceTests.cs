using FluentAssertions;
using Grpc.Core;
using LooksRatingApi.Contracts.WritingOffSparks;
using LooksRatingApi.Services.GrpcService;
using LooksRatingGrpc;
using LooksRatingApi.Tests.Infrastructure.Helpers;

namespace LooksRatingApi.Tests.Unit.Services.WritingOffSparks;

public sealed class GetWritingsOffSparksGrpcServiceTests
{
    [Fact]
    public async Task GetWritingsOffSparks_ReturnsFailure_WhenCityIsEmpty()
    {
        var orchestrator = Substitute.For<IGetWritingsOffSparksOrchestrator>();
        var service = new GetWritingsOffSparksGrpcService(orchestrator, GrpcTestAuth.Disabled());

        var response = await service.GetWritingsOffSparks(
            new GetWritingsOffSparksRequest { City = "   " },
            CreateContext());

        response.Success.Should().BeFalse();
        response.Message.Should().Be("Город не указан");
        await orchestrator.DidNotReceiveWithAnyArgs().GetByCityAsync(default!, default, default, default);
    }

    [Fact]
    public async Task GetWritingsOffSparks_DelegatesToOrchestrator_WhenRequestIsValid()
    {
        var orchestrator = Substitute.For<IGetWritingsOffSparksOrchestrator>();
        orchestrator
            .GetByCityAsync("moscow", 2, 25, Arg.Any<CancellationToken>())
            .Returns(CSharpFunctionalExtensions.Result.Success(new GetWritingsOffSparksResponse
            {
                Success = true,
                Message = "ok",
            }));

        var service = new GetWritingsOffSparksGrpcService(orchestrator, GrpcTestAuth.Disabled());

        var response = await service.GetWritingsOffSparks(
            new GetWritingsOffSparksRequest
            {
                City = "moscow",
                Page = 2,
                PageSize = 25,
            },
            CreateContext());

        response.Success.Should().BeTrue();
        await orchestrator.Received(1).GetByCityAsync("moscow", 2, 25, Arg.Any<CancellationToken>());
    }

    private static ServerCallContext CreateContext() => Substitute.For<ServerCallContext>();
}
