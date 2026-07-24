using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SmoothAiStockAnalysis.Domain.Documents;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Converters;

/// <summary>
/// Detects and snapshots mutations to a JSON document mapped through
/// <see cref="VersionedDocumentSqliteValueConverter{TDocument}"/>.
/// </summary>
/// <remarks>
/// EF Core uses reference equality and a shared reference snapshot for mutable reference types by
/// default. A document can be edited in place, so comparison and snapshotting must both use the
/// canonical JSON representation. See LADR-015.
/// </remarks>
/// <remarks>
/// The deep-copy snapshot is produced via <see cref="JsonSerializer.Deserialize{T}(string, JsonSerializerOptions?)"/>,
/// so <typeparamref name="TDocument"/> must be JSON-round-trippable (a public parameterless constructor and JSON-serializable members).
/// </remarks>
internal sealed class VersionedDocumentSqliteValueComparer<TDocument> : ValueComparer<TDocument>
    where TDocument : class, IVersionedDocument
{
    /// <summary>
    /// Initializes a comparer using the canonical SQLite JSON serialization contract.
    /// </summary>
    public VersionedDocumentSqliteValueComparer()
        : base(
            (left, right) => AreEqual(left, right),
            document => GetDocumentHashCode(document),
            document => CreateSnapshot(document))
    {
    }

    private static bool AreEqual(TDocument? left, TDocument? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        return left is not null
            && right is not null
            && string.Equals(Serialize(left), Serialize(right), StringComparison.Ordinal);
    }

    private static int GetDocumentHashCode(TDocument document) =>
        document is null ? 0 : StringComparer.Ordinal.GetHashCode(Serialize(document));

    private static TDocument CreateSnapshot(TDocument document)
    {
        return JsonSerializer.Deserialize<TDocument>(Serialize(document), SqliteJsonSerialization.Default)
            ?? throw new InvalidOperationException($"Persisted {typeof(TDocument).Name} JSON must not be null.");
    }

    private static string Serialize(TDocument document) =>
        JsonSerializer.Serialize(document, SqliteJsonSerialization.Default);
}
