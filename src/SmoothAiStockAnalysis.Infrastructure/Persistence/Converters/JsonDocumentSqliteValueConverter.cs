using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Converters;

/// <summary>
/// Persists a versioned structured document as a single canonical JSON <c>TEXT</c> column.
/// </summary>
/// <typeparam name="TDocument">
/// The document type. It must carry an explicit schema-version marker and should preserve unknown
/// members through a <c>[System.Text.Json.Serialization.JsonExtensionData]</c> property.
/// </typeparam>
/// <remarks>
/// Chosen over EF Core's native owned-entity JSON mapping (<c>OwnsOne(...).ToJson()</c>) so the
/// document stays an opaque, self-versioned payload rather than an EF-managed entity graph:
/// <list type="bullet">
///   <item>unknown / forward-compatible fields survive a read-modify-write cycle via the document's
///     extension data, where <c>.ToJson()</c> would silently discard any property absent from the
///     current CLR model;</item>
///   <item>the schema version is a first-class member of the payload (NFR-048), not a scattered
///     owned-entity column;</item>
///   <item>adding a preference is a document-version change, not an EF model migration.</item>
/// </list>
/// The value stays inspectable SQLite text, consistent with the NodaTime mappings in LADR-014, and
/// keeps SQLite the single persistence mechanism required by LADR-002. See LADR-015.
/// </remarks>
internal sealed class JsonDocumentSqliteValueConverter<TDocument> : ValueConverter<TDocument, string>
    where TDocument : class, IVersionedDocument
{
    /// <summary>
    /// Initializes a new instance using the canonical <see cref="SqliteJsonSerialization.Default"/>
    /// serialization contract.
    /// </summary>
    public JsonDocumentSqliteValueConverter()
        : this(SqliteJsonSerialization.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance using the supplied serialization contract.
    /// </summary>
    public JsonDocumentSqliteValueConverter(JsonSerializerOptions options)
        : base(
            document => JsonSerializer.Serialize(document, options),
            json => JsonSerializer.Deserialize<TDocument>(json, options)!)
    {
    }
}
