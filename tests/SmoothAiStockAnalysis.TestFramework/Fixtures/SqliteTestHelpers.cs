using System.Data.Common;

namespace SmoothAiStockAnalysis.TestFramework.Fixtures;

/// <summary>
/// Shared SQLite query helpers for component and integration tests.
/// </summary>
public static class SqliteTestHelpers
{
    /// <summary>
    /// Reads a single scalar value. Supported types: <see cref="string"/>, <see cref="long"/>.
    /// </summary>
    public static async Task<T> ExecuteScalarAsync<T>(
        DbConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            throw new InvalidOperationException("The SQL command returned no value.");
        }
        return (T)Convert.ChangeType(result, typeof(T));
    }
}
