using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Domain;

namespace Clinical.Infrastructure.Data.Configurations;

/// <summary>
/// AI invocation audit log configuration per AIR-S03.
/// Append-only: SQL permission script denies UPDATE/DELETE at database level.
/// </summary>
public sealed class AiInvocationAuditLogConfiguration : IEntityTypeConfiguration<AiInvocationAuditLog>
{
    public void Configure(EntityTypeBuilder<AiInvocationAuditLog> builder)
    {
        builder.ToTable("AiInvocationAuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ActorName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.OperationType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.ResourceType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.ResourceId)
            .HasMaxLength(100);

        builder.Property(a => a.ModelVersion)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.ConfidenceScore)
            .HasPrecision(5, 4);

        builder.Property(a => a.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(50);

        builder.Property(a => a.Timestamp)
            .IsRequired();

        // Indexes for common query patterns
        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AiInvocationAuditLog_Timestamp");

        builder.HasIndex(a => a.ActorId)
            .HasDatabaseName("IX_AiInvocationAuditLog_ActorId");

        builder.HasIndex(a => a.OperationType)
            .HasDatabaseName("IX_AiInvocationAuditLog_OperationType");

        builder.HasIndex(a => new { a.ResourceType, a.ResourceId })
            .HasDatabaseName("IX_AiInvocationAuditLog_Resource");
    }
}
