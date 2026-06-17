using LooksRatingApi;
using LooksRatingApi.Repositories;
using LooksRatingApi.Services.SparksLedger;
using LooksRatingApi.Tests.Infrastructure.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace LooksRatingApi.Tests.Unit.Services.SparksWallet;

public sealed class SparksWalletProvisionerTests
{
    [Fact]
    public async Task EnsureForUserAsync_WhenWalletMissing_PersistsWallet()
    {
        await using var context = CreateContext();
        var user = await TestDataBuilder.SeedUserAsync(context, 901);
        var provisioner = CreateProvisioner(context);

        await provisioner.EnsureForUserAsync(user.Id, CancellationToken.None);

        var wallet = await context.SparksLedgers.SingleOrDefaultAsync(wallet => wallet.UserId == user.Id);
        wallet.Should().NotBeNull();
        wallet!.SparksCount.Should().Be(0m);
    }

    [Fact]
    public async Task EnsureForUserAsync_WhenWalletExists_IsIdempotent()
    {
        await using var context = CreateContext();
        var user = await TestDataBuilder.SeedUserAsync(context, 902);
        var existingWallet = LooksRatingApi.Models.SparksWallet.Create(user.Id, 5m).Value;
        context.SparksLedgers.Add(existingWallet);
        await context.SaveChangesAsync();

        var provisioner = CreateProvisioner(context);
        await provisioner.EnsureForUserAsync(user.Id, CancellationToken.None);

        var wallets = await context.SparksLedgers.Where(wallet => wallet.UserId == user.Id).ToListAsync();
        wallets.Should().ContainSingle();
        wallets[0].SparksCount.Should().Be(5m);
    }

    private static SparksWalletProvisioner CreateProvisioner(LooksRatingDbContext context) =>
        new(new SparksLedgerRepository(context), NullLogger<SparksWalletProvisioner>.Instance);

    private static LooksRatingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LooksRatingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new LooksRatingDbContext(options);
    }
}
