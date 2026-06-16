using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;

namespace SharedKernel.Audit;

/// <summary>
/// EF Core interceptor that automatically captures audit records for PHI-containing entity changes (FR-031).
/// Captures before/after state snapshots for modified entities.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<AuditSaveChangesInterceptor> _logger;
    private readonly Func<string?> _correlationIdAccessor;
    private readonly Func<(Guid? ActorId, string ActorName, string? IpAddress)> _actorAccessor;

    // Entity types that contain PHI and should be audited
    private static readonly HashSet<string> AuditedEntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Patient",
        "User",
        "ClinicalDocument",
        "ExtractedData",
        "PatientView360",
        "DataConflict",
        "InsuranceVerification",
        "Appointment",
        "MedicalCode"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public AuditSaveChangesInterceptor(
        ILogger<AuditSaveChangesInterceptor> logger,
        Func<string?> correlationIdAccessor,
        Func<(Guid? ActorId, string ActorName, string? IpAddress)> actorAccessor)
    {
        _logger = logger;
        _correlationIdAccessor = correlationIdAccessor;
        _actorAccessor = actorAccessor;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context != null)
        {
            CaptureAuditEntries(eventData.Context);
        }
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            CaptureAuditEntries(eventData.Context);
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CaptureAuditEntries(DbContext context)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => ShouldAudit(e.Entity.GetType().Name))
            .ToList();

        if (entries.Count == 0)
            return;

        var (actorId, actorName, ipAddress) = _actorAccessor();
        var correlationId = _correlationIdAccessor();

        foreach (var entry in entries)
        {
            try
            {
                var entityType = entry.Entity.GetType().Name;
                var action = entry.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Modified => "Update",
                    EntityState.Deleted => "Delete",
                    _ => "Unknown"
                };

                // Get entity ID
                var primaryKey = entry.Properties
                    .FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                var resourceId = primaryKey?.CurrentValue?.ToString();

                // Capture before/after state (without raw PHI values - use field change indicators)
                string? beforeState = null;
                string? afterState = null;

                if (entry.State == EntityState.Modified)
                {
                    // Capture changed properties only (not full PHI values)
                    var changes = new Dictionary<string, object?>();
                    foreach (var prop in entry.Properties.Where(p => p.IsModified))
                    {
                        // Redact actual values, just record that field changed
                        changes[prop.Metadata.Name] = new
                        {
                            Changed = true,
                            Type = prop.Metadata.ClrType.Name
                        };
                    }
                    beforeState = JsonSerializer.Serialize(new { ChangedFields = changes.Keys }, JsonOptions);
                    afterState = JsonSerializer.Serialize(new { ChangedFields = changes }, JsonOptions);
                }
                else if (entry.State == EntityState.Added)
                {
                    // Just record that entity was created
                    afterState = JsonSerializer.Serialize(new
                    {
                        Action = "Created",
                        EntityType = entityType,
                        Id = resourceId
                    }, JsonOptions);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    // Record deletion
                    beforeState = JsonSerializer.Serialize(new
                    {
                        Action = "Deleted",
                        EntityType = entityType,
                        Id = resourceId
                    }, JsonOptions);
                }

                var auditLog = new AuditLog
                {
                    ActorId = actorId,
                    ActorName = actorName ?? "System",
                    Action = action,
                    Resource = entityType,
                    ResourceId = resourceId,
                    BeforeState = beforeState,
                    AfterState = afterState,
                    IpAddress = ipAddress,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                };

                // Add audit log to same context transaction
                context.Set<AuditLog>().Add(auditLog);

                _logger.LogDebug(
                    "Audit captured: {Action} {EntityType}/{ResourceId} by {Actor}",
                    action, entityType, resourceId, actorName);
            }
            catch (Exception ex)
            {
                // Don't fail the main operation if audit capture fails
                _logger.LogWarning(ex, "Failed to capture audit for entity {EntityType}", entry.Entity.GetType().Name);
            }
        }
    }

    private static bool ShouldAudit(string entityTypeName)
    {
        // Skip audit log entities themselves to prevent infinite loops
        if (entityTypeName.Contains("AuditLog", StringComparison.OrdinalIgnoreCase))
            return false;

        return AuditedEntityTypes.Contains(entityTypeName);
    }
}
