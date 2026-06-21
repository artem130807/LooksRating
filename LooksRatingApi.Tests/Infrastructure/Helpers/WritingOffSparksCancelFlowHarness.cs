using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Domain.DomainEvents;
using LooksRatingApi.Enums;
using LooksRatingApi.Messages.Kafka.PhotoRated.Producers.EventProducer;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.GrpcService;
using LooksRatingApi.Services.Orchestrators;
using LooksRatingApi.Services.SparksLedger;
using LooksRatingApi.Services.SparksWallet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LooksRatingApi.Tests.Infrastructure.Helpers;

/// <summary>
/// Wires real sparks wallet services and WritingOffSparks orchestrators for end-to-end flow tests.
/// Mirrors the TicketBot cancel path: <c>WritingOffSparksGrpcClient.mark_cancelled</c> → gRPC → orchestrator.
/// </summary>
internal sealed class WritingOffSparksCancelFlowHarness : IAsyncDisposable
{
    private WritingOffSparksCancelFlowHarness(LooksRatingDbContext context)
    {
        Context = context;
        SparksLedgerRepository = new SparksLedgerRepository(context);
        WritingOffSparksRepository = new WritingOffSparksRepository(context);
        UserRepository = new SparksFlowUserRepository(context);

        var eventStore = Substitute.For<IEventStoreRepository>();
        eventStore
            .SaveEventsAsync(
                Arg.Any<Guid>(),
                Arg.Any<IEnumerable<LooksRatingApi.Domain.Base.DomainEvent>>())
            .Returns(Task.CompletedTask);

        var debitProducer = Substitute.For<IKafkaEventProducer<CurrencyDebitedEvent>>();
        debitProducer
            .Produce(Arg.Any<CurrencyDebitedEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var compensationProducer = Substitute.For<IKafkaEventProducer<CurrencyDebitCompensatedEvent>>();
        compensationProducer
            .Produce(Arg.Any<CurrencyDebitCompensatedEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var changeSparksLedgersService = new ChangeSparksLedgersService(SparksLedgerRepository);
        var eventDispatcher = new SparksLedgerEventDispatcher(changeSparksLedgersService);

        var currencyDebitedService = new CurrencyDebitedService(
            debitProducer,
            SparksLedgerRepository,
            eventStore,
            UserRepository);

        var currencyDebitCompensatedService = new CurrencyDebitCompensatedService(
            compensationProducer,
            eventDispatcher,
            SparksLedgerRepository,
            eventStore);

        var sparksDebitIdempotencyRepository = new SparksDebitIdempotencyRepository(context);
        var orphanDebitResolver = new SparksOrphanDebitResolver(
            sparksDebitIdempotencyRepository,
            WritingOffSparksRepository,
            currencyDebitCompensatedService,
            SparksLedgerRepository,
            context,
            NullLogger<SparksOrphanDebitResolver>.Instance);

        DebitedSparksOrchestrator = new DebitedSparksOrchestrator(
            currencyDebitedService,
            NullLogger<DebitedSparksOrchestrator>.Instance,
            context,
            UserRepository,
            SparksLedgerRepository,
            sparksDebitIdempotencyRepository,
            WritingOffSparksRepository,
            orphanDebitResolver);

        RollBackDebitedSparksOrchestrator = new RollBackDebitedSparksOrchestrator(
            currencyDebitCompensatedService,
            eventStore,
            NullLogger<RollBackDebitedSparksOrchestrator>.Instance,
            context,
            UserRepository,
            SparksLedgerRepository,
            new SparksDebitIdempotencyRepository(context));

        CreateWritingOffSparksOrchestrator = new CreateWritingOffSparksOrchestrator(
            WritingOffSparksRepository,
            UserRepository,
            context,
            NullLogger<CreateWritingOffSparksOrchestrator>.Instance,
            new PhotoProfileRepository(context),
            new SeasonRepository(context),
            sparksDebitIdempotencyRepository);

        var updateStatusOrchestrator = new UpdateStatusWritingOffSparksOrchestrator(
            WritingOffSparksRepository,
            currencyDebitCompensatedService,
            SparksLedgerRepository,
            new SparksDebitIdempotencyRepository(context),
            context,
            NullLogger<UpdateStatusWritingOffSparksOrchestrator>.Instance);

        UpdateStatusGrpcService = new UpdateStatusWritingOffSparksGrpcService(
            updateStatusOrchestrator,
            GrpcTestAuth.Disabled());
    }

    public LooksRatingDbContext Context { get; }

    public SparksLedgerRepository SparksLedgerRepository { get; }

    public WritingOffSparksRepository WritingOffSparksRepository { get; }

    public SparksFlowUserRepository UserRepository { get; }

    public DebitedSparksOrchestrator DebitedSparksOrchestrator { get; }

    public RollBackDebitedSparksOrchestrator RollBackDebitedSparksOrchestrator { get; }

    public CreateWritingOffSparksOrchestrator CreateWritingOffSparksOrchestrator { get; }

    public UpdateStatusWritingOffSparksGrpcService UpdateStatusGrpcService { get; }

    public static WritingOffSparksCancelFlowHarness Create() =>
        new(CreateContext());

    public Task<decimal> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default) =>
        SparksLedgerRepository.GetBalanceAsync(userId, cancellationToken);

    public ValueTask DisposeAsync() => Context.DisposeAsync();

    public static LooksRatingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new LooksRatingDbContext(options);
    }
}
