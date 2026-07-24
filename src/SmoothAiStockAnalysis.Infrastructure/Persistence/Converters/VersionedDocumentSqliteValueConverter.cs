using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmoothAiStockAnalysis.Domain.Documents;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Converters;

/// <summary>
/// Persists a versioned structured document as a single canonical JSON <c>TEXT</c> column.
/// </summary>
/// <typeparam name="TDocument">
/// The document type. It must carry an explicit schema-version marker and should preserve unknown
/// members through a <c>[System.Text.Json.Serialization.JsonExtensionData]</c> property.
/// </typeparam>
/// <remarks>
/// Chosen for metadata that is read and written as one opaque, self-versioned payload rather
/// than as an EF-managed entity graph:
/// <list type="bullet">
///   <item>unknown / forward-compatible fields survive a read-modify-write cycle via the document's
///     extension data;</item>
///   <item>the schema version is a first-class member of the payload (NFR-048), not a scattered
///     owned-entity column;</item>
///   <item>adding a preference is a document-version change, not an EF model migration.</item>
/// </list>
/// The value stays inspectable SQLite text, consistent with the NodaTime mappings in LADR-014, and
/// keeps SQLite the single persistence mechanism required by LADR-002. The converter is typed
/// <c>ValueConverter&lt;TDocument, string&gt;</c> and the target column is therefore assumed to be
/// non-nullable; a SQL <c>NULL</c> will surface as an <see cref="InvalidOperationException"/> from
/// <see cref="Deserialize"/>. A future <c>TDocument?</c> mapping must add an explicit null-handling
/// step. See LADR-015.
/// </remarks>
internal sealed class VersionedDocumentSqliteValueConverter<TDocument> : ValueConverter<TDocument, string>
    where TDocument : class, IVersionedDocument
{
    /// <summary>
    /// Initializes a new instance using the canonical SQLite JSON serialization contract.
    /// </summary>
    public VersionedDocumentSqliteValueConverter()
        : base(
            document => Serialize(document),
            json => Deserialize(json))
    {
    }

    private static string Serialize(TDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return System.Text.Json.JsonSerializer.Serialize(document, SqliteJsonSerialization.Default);
    }

    private static TDocument Deserialize(string json) =>
        System.Text.Json.JsonSerializer.Deserialize<TDocument>(json, SqliteJsonSerialization.Default)
        ?? throw new InvalidOperationException($"Persisted {typeof(TDocument).Name} JSON must not be null.");
}
