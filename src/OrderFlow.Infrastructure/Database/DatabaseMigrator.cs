using DbUp;
using Npgsql;

namespace OrderFlow.Infrastructure.Database;

public static class DatabaseMigrator
{
    // Arbitrary constant key; serializes concurrent migrations (parallel test hosts, multiple instances).
    private const long MigrationLockKey = 7_215_001_001;

    public static void Migrate(string connectionString)
    {
        using var lockConnection = new NpgsqlConnection(connectionString);
        lockConnection.Open();

        using (var acquire = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", lockConnection))
        {
            acquire.Parameters.AddWithValue("key", MigrationLockKey);
            acquire.ExecuteNonQuery();
        }

        // The session-level lock is released when lockConnection is disposed.
        RunUpgrade(connectionString);
    }

    private static void RunUpgrade(string connectionString)
    {
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly)
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            throw new InvalidOperationException("Database migration failed.", result.Error);
        }
    }
}
