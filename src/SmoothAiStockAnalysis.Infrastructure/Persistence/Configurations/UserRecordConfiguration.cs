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
        builder.ToTable("users");

        builder.HasKey(user => user.Id)
            .HasName("pk_users");

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(user => user.UniqueIdentifier)
            .HasColumnName("unique_identifier")
            .IsRequired();

        builder.HasIndex(user => user.UniqueIdentifier)
            .IsUnique()
            .HasDatabaseName("ux_users_unique_identifier");

        PropertyBuilder<UserMetadataDocument> metadataProperty = builder.Property(user => user.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("TEXT")
            .IsRequired()
            .HasConversion(new VersionedDocumentSqliteValueConverter<UserMetadataDocument>());

        metadataProperty.Metadata.SetValueComparer(
            new VersionedDocumentSqliteValueComparer<UserMetadataDocument>());
    }
}
