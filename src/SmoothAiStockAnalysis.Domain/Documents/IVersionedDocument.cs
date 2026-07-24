namespace SmoothAiStockAnalysis.Domain.Documents;

/// <summary>
/// Contract for a domain document whose serialized shape changes independently of its database
/// column.
/// </summary>
/// <remarks>
/// The explicit version marker allows a later reader to interpret a persisted document without
/// guessing its shape. Persistence adapters serialize the marker; the Domain owns its meaning.
/// </remarks>
public interface IVersionedDocument
{
    /// <summary>
    /// Gets the version of this document's serialized contract.
    /// </summary>
    int SchemaVersion { get; }
}
