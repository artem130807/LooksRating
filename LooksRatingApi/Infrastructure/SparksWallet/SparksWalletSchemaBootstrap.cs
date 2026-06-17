using Microsoft.EntityFrameworkCore;

namespace LooksRatingApi.Infrastructure.SparksWallet
{
    public static class SparksWalletSchemaBootstrap
    {
        public static async Task EnsureAsync(LooksRatingDbContext dbContext, CancellationToken cancellationToken = default)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'SparksLedger' AND column_name = 'Amount'
                    ) AND NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'SparksLedger' AND column_name = 'SparksCount'
                    ) THEN
                        ALTER TABLE "SparksLedger" RENAME COLUMN "Amount" TO "SparksCount";
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'SparksLedger' AND column_name = 'Type'
                    ) THEN
                        ALTER TABLE "SparksLedger" DROP COLUMN "Type";
                    END IF;
                END $$;
                """,
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                DROP INDEX IF EXISTS "IX_SparksLedger_UserId_Type_CreatedAt";
                """,
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'User' AND column_name = 'SparksCount'
                    ) THEN
                        ALTER TABLE "User" DROP COLUMN "SparksCount";
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'User' AND column_name = 'SparksWalletId'
                    ) THEN
                        ALTER TABLE "User" DROP COLUMN "SparksWalletId";
                    END IF;
                END $$;
                """,
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_SparksLedger_UserId"
                    ON "SparksLedger" ("UserId");
                """,
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "EventStores" (
                    "Id" uuid NOT NULL,
                    "AggregateId" uuid NOT NULL,
                    "EventType" character varying(256) NOT NULL,
                    "EventData" text NOT NULL,
                    "Version" integer NOT NULL,
                    "OccurredAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_EventStores" PRIMARY KEY ("Id")
                );
                """,
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_EventStores_AggregateId_Version"
                    ON "EventStores" ("AggregateId", "Version");
                """,
                cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS "IX_EventStores_OccurredAt"
                    ON "EventStores" ("OccurredAt");
                """,
                cancellationToken);
        }
    }
}
