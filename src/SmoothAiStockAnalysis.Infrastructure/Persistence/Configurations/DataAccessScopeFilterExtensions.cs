using Microsoft.EntityFrameworkCore;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Applies the global user-isolation query filter to the tenant root and every user-owned entity.
/// </summary>
/// <remarks>
/// Each filter references <see cref="SmoothAiStockAnalysisDbContext.UserIsolationTenantKey"/> on a
/// context instance. EF Core translates a member access on the context into a per-query evaluation
/// against the <em>current</em> context, so every unit of work filters on its own scope rather
/// than a cached first value. A non-null key filters rows to that user; <see langword="null"/>
/// (the named system scope) short-circuits the predicate so the filter is a no-op. Entities
/// without the <see cref="UserOwnedEntityTypeBuilderExtensions.IsUserOwnedAnnotation"/>
/// annotation — shared reference data — receive no filter and stay queryable in every scope
/// (NFR-040 / BR-48).
/// </remarks>
internal static class DataAccessScopeFilterExtensions
{
    /// <summary>
    /// The context instance the isolation filter reads its tenant key from. Declared as a static
    /// so the filter lambdas below reference the member on the ambient context type; EF Core
    /// substitutes the live context during query compilation.
    /// </summary>
    private static readonly SmoothAiStockAnalysisDbContext Context = null!;

    /// <summary>
    /// Applies the user-isolation filter to <see cref="UserRecord"/> and all annotated owned dependents.
    /// </summary>
    internal static void ApplyDataAccessScopeFilters(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<UserRecord>()
            .HasQueryFilter(user => Context.UserIsolationTenantKey == null || user.Id == Context.UserIsolationTenantKey);

        foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            bool isUserOwned = entityType.FindAnnotation(
                UserOwnedEntityTypeBuilderExtensions.IsUserOwnedAnnotation)?.Value as bool? == true;

            if (!isUserOwned || entityType.ClrType == typeof(UserRecord))
            {
                continue;
            }

            ApplyOwnedDependentFilter(modelBuilder, entityType.ClrType);
        }
    }

    private static void ApplyOwnedDependentFilter(ModelBuilder modelBuilder, Type clrType)
    {
        System.Reflection.MethodInfo method = typeof(DataAccessScopeFilterExtensions)
            .GetMethod(nameof(ApplyOwnedDependentFilterGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(clrType);

        method.Invoke(null, [modelBuilder]);
    }

    private static void ApplyOwnedDependentFilterGeneric<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class
    {
        string userIdProperty = UserOwnedEntityTypeBuilderExtensions.OwnershipForeignKeyName;
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(entity =>
                Context.UserIsolationTenantKey == null
                || EF.Property<long>(entity, userIdProperty) == Context.UserIsolationTenantKey);
    }
}
