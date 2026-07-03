using LooksRatingApi.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackExchange.Redis;

namespace LooksRatingApi.Infrastructure.DeployMigrations
{
    public sealed class DeployMigrationRunner
    {
        private static readonly TimeSpan MigrationLockTtl = TimeSpan.FromMinutes(60);
        private static readonly TimeSpan PeerWaitTimeout = TimeSpan.FromMinutes(60);
        private static readonly TimeSpan PeerPollInterval = TimeSpan.FromSeconds(2);

        private readonly LooksRatingDbContext _context;
        private readonly IEnumerable<IDeployMigration> _migrations;
        private readonly IDatabase _redis;
        private readonly ILogger<DeployMigrationRunner> _logger;

        public DeployMigrationRunner(
            LooksRatingDbContext context,
            IEnumerable<IDeployMigration> migrations,
            IConnectionMultiplexer redis,
            ILogger<DeployMigrationRunner> logger)
        {
            _context = context;
            _migrations = migrations;
            _redis = redis.GetDatabase();
            _logger = logger;
        }

        public async Task RunPendingAsync(CancellationToken cancellationToken = default)
        {
            await EnsureHistoryTableAsync(cancellationToken);

            foreach (var migration in _migrations.OrderBy(m => m.Name, StringComparer.Ordinal))
            {
                if (await IsAppliedAsync(migration.Name, cancellationToken))
                {
                    _logger.LogDebug("Deploy migration {Name} already applied", migration.Name);
                    continue;
                }

                var lockKey = $"deploy-migration:lock:{migration.Name}";
                var lockToken = Guid.NewGuid().ToString("N");
                var lockAcquired = await _redis.LockTakeAsync(lockKey, lockToken, MigrationLockTtl);
                if (!lockAcquired)
                {
                    _logger.LogInformation(
                        "Deploy migration {Name} is running on another instance, waiting...",
                        migration.Name);

                    if (await WaitForPeerAsync(migration.Name, cancellationToken))
                    {
                        continue;
                    }

                    _logger.LogWarning(
                        "Deploy migration {Name} peer did not finish in time; attempting to take over",
                        migration.Name);
                    lockAcquired = await _redis.LockTakeAsync(lockKey, lockToken, MigrationLockTtl);
                    if (!lockAcquired)
                    {
                        throw new InvalidOperationException(
                            $"Deploy migration '{migration.Name}' could not be completed or taken over.");
                    }
                }

                try
                {
                    if (await IsAppliedAsync(migration.Name, cancellationToken))
                    {
                        continue;
                    }

                    _logger.LogInformation("Applying deploy migration {Name}...", migration.Name);

                    var completed = await migration.ApplyAsync(cancellationToken);
                    if (!completed)
                    {
                        _logger.LogInformation(
                            "Deploy migration {Name} skipped and will retry on next startup",
                            migration.Name);
                        continue;
                    }

                    _context.DeployMigrationHistories.Add(new DeployMigrationHistory
                    {
                        Name = migration.Name,
                        AppliedAt = DateTime.UtcNow,
                    });

                    try
                    {
                        await _context.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Deploy migration {Name} applied successfully", migration.Name);
                    }
                    catch (DbUpdateException ex) when (IsUniqueViolation(ex))
                    {
                        _logger.LogInformation(
                            "Deploy migration {Name} history already recorded by another instance",
                            migration.Name);
                        _context.ChangeTracker.Clear();
                    }
                }
                finally
                {
                    await _redis.LockReleaseAsync(lockKey, lockToken);
                }
            }
        }

        private async Task<bool> IsAppliedAsync(string name, CancellationToken cancellationToken) =>
            await _context.DeployMigrationHistories
                .AsNoTracking()
                .AnyAsync(x => x.Name == name, cancellationToken);

        private async Task EnsureHistoryTableAsync(CancellationToken cancellationToken)
        {
            const string sql = """
                CREATE TABLE IF NOT EXISTS "DeployMigrationHistory" (
                    "Name" character varying(256) NOT NULL,
                    "AppliedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_DeployMigrationHistory" PRIMARY KEY ("Name")
                );
                """;

            await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }

        /// <returns>true when migration was applied by a peer instance.</returns>
        private async Task<bool> WaitForPeerAsync(string name, CancellationToken cancellationToken)
        {
            var deadline = DateTime.UtcNow + PeerWaitTimeout;
            while (DateTime.UtcNow < deadline)
            {
                if (await IsAppliedAsync(name, cancellationToken))
                {
                    return true;
                }

                await Task.Delay(PeerPollInterval, cancellationToken);
            }

            return await IsAppliedAsync(name, cancellationToken);
        }

        private static bool IsUniqueViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
