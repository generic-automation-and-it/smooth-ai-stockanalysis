using System.Data.Common;
using System.Globalization;

namespace SmoothAiStockAnalysis.TestFramework.Fixtures;

/// <summary>
/// Shared SQLite query helpers for component and integration tests.
/// </summary>
public static class SqliteTestHelpers
{
    /// <summary>
    /// Reads a single scalar value, converting it to <typeparamref name="T"/> via
    /// <see cref="Convert.ChangeType(object?, Type)"/>. Any <see cref="IConvertible"/>
    /// target type that the underlying scalar can represent is supported
    /// (e.g. <see cref="string"/>, <see cref="long"/>, <see cref="int"/>, <see cref="bool"/>).
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
        return (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture);
    }
}
