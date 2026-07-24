namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Converters;

/// <summary>
/// Contract for a structured payload persisted as a single JSON <c>TEXT</c> column.
/// </summary>
/// <remarks>
/// Every document stored through <see cref="JsonDocumentSqliteValueConverter{TDocument}"/> must
/// carry an explicit schema-version marker (NFR-048) so a stored payload can be interpreted by a
/// later reader without guessing its shape. The marker is a first-class member of the document
/// rather than a side column, keeping the serialized contract self-describing. See LADR-015.
/// </remarks>
internal interface IVersionedDocument
{
    /// <summary>
    /// Gets the schema version of the serialized document. Increment it when the document's
    /// serialization contract changes so readers can branch on the version they encounter.
    /// </summary>
    int SchemaVersion { get; }
}
