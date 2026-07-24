using Microsoft.Data.Sqlite;

namespace SmoothAiStockAnalysis.TestFramework.Fixtures;

/// <summary>
/// An isolated, on-disk SQLite database for component and integration tests.
/// </summary>
public sealed class SqliteTestDatabase : IAsyncDisposable
{
    public SqliteTestDatabase()
    {
        string directoryPath = Path.Combine(Path.GetTempPath(), "smooth-ai-stockanalysis-tests");
        Directory.CreateDirectory(directoryPath);

        DatabasePath = Path.Combine(directoryPath, $"{Guid.NewGuid():N}.db");
        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Pooling = false
        }.ConnectionString;
    }

    public string ConnectionString { get; }

    public string DatabasePath { get; }

    public ValueTask DisposeAsync()
    {
        TryDelete(DatabasePath);
        TryDelete(DatabasePath + "-wal");
        TryDelete(DatabasePath + "-shm");
        TryDelete(DatabasePath + "-journal");
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
