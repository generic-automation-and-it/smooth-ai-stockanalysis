using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Converters;
using SmoothAiStockAnalysis.Infrastructure.Persistence.Entities;

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the user tenant-root persistence record.
/// </summary>
internal sealed class UserRecordConfiguration : IEntityTypeConfiguration<UserRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserRecord> builder)
    {
        // Relational names come from the global snake_case naming convention (LADR-016);
        // this class only carries non-naming configuration so the convention stays authoritative.
        builder.Property(user => user.Id)
            .ValueGeneratedOnAdd();

        builder.Property(user => user.UniqueIdentifier)
            .IsRequired();

        builder.HasIndex(user => user.UniqueIdentifier)
            .IsUnique();

        PropertyBuilder<UserMetadataDocument> metadataProperty = builder.Property(user => user.Metadata)
            .HasColumnType("TEXT")
            .IsRequired()
            .HasConversion(new VersionedDocumentSqliteValueConverter<UserMetadataDocument>());

        metadataProperty.Metadata.SetValueComparer(
            new VersionedDocumentSqliteValueComparer<UserMetadataDocument>());
    }
}
