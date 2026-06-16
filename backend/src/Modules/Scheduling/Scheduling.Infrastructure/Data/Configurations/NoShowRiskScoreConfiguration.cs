using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scheduling.Domain.Entities;

namespace Scheduling.Infrastructure.Data.Configurations;

public sealed class NoShowRiskScoreConfiguration : IEntityTypeConfiguration<NoShowRiskScore>
{
    public void Configure(EntityTypeBuilder<NoShowRiskScore> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.PatientId)
            .IsRequired();

        builder.HasIndex(n => n.PatientId)
            .HasDatabaseName("IX_NoShowRiskScore_PatientId");

        // Score 0-100 CHECK constraint
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_NoShowRiskScore_Score",
            "[Score] >= 0 AND [Score] <= 100"));

        builder.Property(n => n.RiskFactors)
            .HasColumnType("nvarchar(max)");

        builder.Property(n => n.CalculatedAt)
            .IsRequired();

        builder.HasIndex(n => n.AppointmentId)
            .IsUnique()
            .HasDatabaseName("IX_NoShowRiskScore_AppointmentId");

        builder.HasOne(n => n.Appointment)
            .WithMany()
            .HasForeignKey(n => n.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
