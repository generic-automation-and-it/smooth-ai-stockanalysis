using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Shared EF configuration helpers for user-owned dependent tables.
/// </summary>
/// <remarks>
/// Worktask 02 establishes the ownership and composite-uniqueness convention before any
/// production owned dependents exist. Feature owners must use these helpers rather than
/// hand-rolling a global unique natural key.
/// </remarks>
internal static class UserOwnedEntityTypeBuilderExtensions
{
    /// <summary>
    /// CLR and relational ownership foreign-key name on every user-owned dependent.
    /// </summary>
    public const string OwnershipForeignKeyName = "UserId";

    /// <summary>
    /// Configures a required ownership FK to the tenant-root <see cref="UserRecord"/> with
    /// restrictive delete semantics.
    /// </summary>
    public static EntityTypeBuilder<TEntity> ConfigureUserOwnedDependent<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property<long>(OwnershipForeignKeyName).IsRequired();

        builder.HasOne<UserRecord>()
            .WithMany()
            .HasForeignKey(OwnershipForeignKeyName)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        return builder;
    }

    /// <summary>
    /// Creates a unique index that always starts with the ownership key, then the natural key.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="naturalKeyPropertyNames">
    /// Natural-key CLR property names only. Do not include <see cref="OwnershipForeignKeyName"/>;
    /// it is prepended automatically so a competing global unique natural key cannot be formed
    /// through this helper.
    /// </param>
    public static IndexBuilder HasUserScopedUniqueIndex<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        params string[] naturalKeyPropertyNames)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(naturalKeyPropertyNames);

        if (naturalKeyPropertyNames.Length == 0)
        {
            throw new ArgumentException(
                "A user-owned natural unique index requires at least one natural-key property after the ownership key.",
                nameof(naturalKeyPropertyNames));
        }

        foreach (string propertyName in naturalKeyPropertyNames)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentException(
                    "Natural-key property names must be non-empty.",
                    nameof(naturalKeyPropertyNames));
            }

            if (string.Equals(propertyName, OwnershipForeignKeyName, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Do not include '{OwnershipForeignKeyName}' in the natural-key list; it is prepended automatically.",
                    nameof(naturalKeyPropertyNames));
            }
        }

        string[] indexProperties = [OwnershipForeignKeyName, .. naturalKeyPropertyNames];
        return builder.HasIndex(indexProperties).IsUnique();
    }
}
