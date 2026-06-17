using Microsoft.Extensions.Options;
using Npgsql;

namespace LooksRatingApi.Infrastructure.Quartz
{
    public sealed class QuartzSchemaBootstrap
    {
        private readonly IConfiguration _configuration;
        private readonly LooksRatingQuartzOptions _options;
        private readonly ILogger<QuartzSchemaBootstrap> _logger;

        public QuartzSchemaBootstrap(
            IConfiguration configuration,
            IOptions<LooksRatingQuartzOptions> options,
            ILogger<QuartzSchemaBootstrap> logger)
        {
            _configuration = configuration;
            _options = options.Value;
            _logger = logger;
        }

        public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
        {
            if (!_options.UseClustering || !_options.AutoCreateSchema)
                return;

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string DefaultConnection is required for Quartz clustering.");

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            if (await TableExistsAsync(connection, cancellationToken))
            {
                _logger.LogDebug("Quartz schema already exists");
                return;
            }

            var script = await ReadEmbeddedScriptAsync(cancellationToken);
            foreach (var statement in SplitSqlStatements(script))
            {
                await using var command = new NpgsqlCommand(statement, connection);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            _logger.LogInformation("Quartz PostgreSQL schema created");
        }

        private static async Task<bool> TableExistsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = current_schema()
                  AND table_name = 'qrtz_locks'
                LIMIT 1
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null;
        }

        private static IEnumerable<string> SplitSqlStatements(string script)
        {
            return script
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(statement => !string.IsNullOrWhiteSpace(statement));
        }

        private static async Task<string> ReadEmbeddedScriptAsync(CancellationToken cancellationToken)
        {
            var assembly = typeof(QuartzSchemaBootstrap).Assembly;
            const string resourceName = "LooksRatingApi.Infrastructure.Quartz.quartz_postgres_create.sql";

            await using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource {resourceName} was not found.");

            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(cancellationToken);
        }
    }
}
