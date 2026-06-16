using Clinical.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinical.Infrastructure.Data.Configurations;

public sealed class InsuranceVerificationConfiguration : IEntityTypeConfiguration<InsuranceVerification>
{
    public void Configure(EntityTypeBuilder<InsuranceVerification> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.AppointmentId)
            .IsRequired();

        builder.HasIndex(v => v.AppointmentId)
            .HasDatabaseName("IX_InsuranceVerification_AppointmentId");

        builder.Property(v => v.InsuranceName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.MemberId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(v => v.Status)
            .IsRequired();

        builder.Property(v => v.StatusMessage)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
