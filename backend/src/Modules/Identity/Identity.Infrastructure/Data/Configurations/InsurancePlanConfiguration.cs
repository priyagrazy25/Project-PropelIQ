using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public sealed class InsurancePlanConfiguration : IEntityTypeConfiguration<InsurancePlan>
{
    public void Configure(EntityTypeBuilder<InsurancePlan> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InsuranceName)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(i => i.InsuranceName)
            .IsUnique()
            .HasDatabaseName("IX_InsurancePlan_InsuranceName");

        builder.Property(i => i.ValidMemberIdPattern)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(i => i.IsActive)
            .HasDefaultValue(true);

        // Soft-delete query filter (DR-008)
        builder.HasQueryFilter(i => !i.IsDeleted);

        // Seed predefined dummy insurance records (DR-017)
        var seedDate = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new
            {
                Id = new Guid("a1b2c3d4-0001-0000-0000-000000000001"),
                InsuranceName = "Blue Cross Blue Shield",
                ValidMemberIdPattern = @"^[A-Z]{3}\d{9}$",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new
            {
                Id = new Guid("a1b2c3d4-0002-0000-0000-000000000002"),
                InsuranceName = "Aetna",
                ValidMemberIdPattern = @"^\d{8,12}$",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new
            {
                Id = new Guid("a1b2c3d4-0003-0000-0000-000000000003"),
                InsuranceName = "UnitedHealthcare",
                ValidMemberIdPattern = @"^U\d{9}$",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new
            {
                Id = new Guid("a1b2c3d4-0004-0000-0000-000000000004"),
                InsuranceName = "Cigna",
                ValidMemberIdPattern = @"^\d{10}$",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new
            {
                Id = new Guid("a1b2c3d4-0005-0000-0000-000000000005"),
                InsuranceName = "Humana",
                ValidMemberIdPattern = @"^H\d{8}$",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new
            {
                Id = new Guid("a1b2c3d4-0006-0000-0000-000000000006"),
                InsuranceName = "Kaiser Permanente",
                ValidMemberIdPattern = @"^\d{8,10}$",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new
            {
                Id = new Guid("a1b2c3d4-0007-0000-0000-000000000007"),
                InsuranceName = "Anthem",
                ValidMemberIdPattern = @"^[A-Z]{2}\d{9}$",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new
            {
                Id = new Guid("a1b2c3d4-0008-0000-0000-000000000008"),
                InsuranceName = "Molina Healthcare",
                ValidMemberIdPattern = @"^\d{9,12}$",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );
    }
}
