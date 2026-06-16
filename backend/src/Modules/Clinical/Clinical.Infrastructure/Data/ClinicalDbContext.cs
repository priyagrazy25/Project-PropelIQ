using Clinical.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Data;
using SharedKernel.Domain;

namespace Clinical.Infrastructure.Data;

public sealed class ClinicalDbContext : BaseDbContext
{
    public ClinicalDbContext(DbContextOptions<ClinicalDbContext> options) : base(options)
    {
    }

    public DbSet<ClinicalDocument> ClinicalDocuments => Set<ClinicalDocument>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<ExtractedData> ExtractedData => Set<ExtractedData>();
    public DbSet<PatientView360> PatientViews360 => Set<PatientView360>();
    public DbSet<DataConflict> DataConflicts => Set<DataConflict>();
    public DbSet<MedicalCode> MedicalCodes => Set<MedicalCode>();
    public DbSet<Icd10Code> Icd10Codes => Set<Icd10Code>();
    public DbSet<CptCode> CptCodes => Set<CptCode>();
    public DbSet<DocumentEmbedding> DocumentEmbeddings => Set<DocumentEmbedding>();
    public DbSet<InsuranceVerification> InsuranceVerifications => Set<InsuranceVerification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AiInvocationAuditLog> AiInvocationAuditLogs => Set<AiInvocationAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("clinical");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicalDbContext).Assembly);
    }
}
