using Microsoft.Data.Sqlite;

namespace SmoothAiStockAnalysis.TestFramework.Fixtures;

/// <summary>
/// An isolated, on-disk SQLite database for component and integration tests.
/// </summary>
public sealed class SqliteTestDatabase : IAsyncDisposable
{
    private SqliteTestDatabase(string databasePath)
    {
        DatabasePath = databasePath;
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false
        }.ConnectionString;
    }

    public string ConnectionString { get; }

    public string DatabasePath { get; }

    public static SqliteTestDatabase Create()
    {
        string directoryPath = Path.Combine(Path.GetTempPath(), "smooth-ai-stockanalysis-tests");
        Directory.CreateDirectory(directoryPath);

        return new SqliteTestDatabase(Path.Combine(directoryPath, $"{Guid.NewGuid():N}.db"));
    }

    public ValueTask DisposeAsync()
    {
        TryDelete(DatabasePath);
        TryDelete(DatabasePath + "-wal");
        TryDelete(DatabasePath + "-shm");
        return ValueTask.CompletedTask;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // best-effort cleanup; lingering file is acceptable
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
