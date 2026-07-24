using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Converters;

/// <summary>
/// The canonical <see cref="JsonSerializerOptions"/> for documents persisted as SQLite JSON text.
/// </summary>
/// <remarks>
/// These options are the stored serialization contract, not a rendering preference: changing them
/// changes the on-disk representation of every persisted document. Property names are camelCased
/// on write and matched case-insensitively on read, numeric properties can be read from string
/// JSON tokens (for example, quoted integers), output is compact (the column is a payload, not a
/// human document), and unknown members are deliberately <b>not</b> disallowed so a document's
/// <c>[JsonExtensionData]</c> member can retain forward-compatible fields written by a newer
/// schema version. See LADR-015.
/// </remarks>
internal static class SqliteJsonSerialization
{
    /// <summary>
    /// Gets the shared options instance. It is read-only after construction and safe to reuse.
    /// </summary>
    internal static JsonSerializerOptions Default { get; } = CreateDefault();

    private static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            WriteIndented = false,
        };
        options.MakeReadOnly();
        return options;
    }
}
