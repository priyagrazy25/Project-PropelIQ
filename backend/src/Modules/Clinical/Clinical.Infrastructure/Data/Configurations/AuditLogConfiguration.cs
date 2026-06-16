using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Domain;

namespace Clinical.Infrastructure.Data.Configurations;

/// <summary>
/// Append-only audit log configuration (DR-011).
/// SQL permission script denies UPDATE/DELETE at the database level.
/// </summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ActorName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Resource)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.ResourceId)
            .HasMaxLength(100);

        builder.Property(a => a.BeforeState)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.AfterState)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45);

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(50);

        builder.Property(a => a.Timestamp)
            .IsRequired();

        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AuditLog_Timestamp");

        builder.HasIndex(a => a.ActorId)
            .HasDatabaseName("IX_AuditLog_ActorId");

        builder.HasIndex(a => new { a.Resource, a.ResourceId })
            .HasDatabaseName("IX_AuditLog_Resource");
    }
}
