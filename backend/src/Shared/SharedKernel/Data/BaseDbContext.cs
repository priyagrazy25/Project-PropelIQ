using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;

namespace SharedKernel.Data;

public abstract class BaseDbContext : DbContext
{
    protected BaseDbContext(DbContextOptions options) : base(options)
    {
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        ApplySoftDeleteTimestamp();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.GetType()
                    .GetProperty(nameof(BaseEntity.UpdatedAt))!
                    .SetValue(entry.Entity, DateTime.UtcNow);
            }
        }
    }

    private void ApplySoftDeleteTimestamp()
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified
                        && e.Entity.IsDeleted
                        && e.Property(nameof(BaseEntity.IsDeleted)).IsModified);

        foreach (var entry in entries)
        {
            entry.Entity.DeletedAt ??= DateTime.UtcNow;
        }
    }
}
