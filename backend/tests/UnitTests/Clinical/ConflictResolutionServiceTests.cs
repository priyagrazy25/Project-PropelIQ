using Clinical.Application.Abstractions;
using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Clinical.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel.Caching;

namespace UnitTests.Clinical;

public sealed class ConflictResolutionServiceTests
{
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<ILogger<ConflictResolutionService>> _logger = new();

    [Fact]
    public async Task ResolveConflictAsync_OpenConflict_ResolvesAndWritesAuditAndInvalidatesCache()
    {
        await using var db = CreateDbContext();

        var patientId = Guid.NewGuid();
        var conflict = new DataConflict
        {
            PatientId = patientId,
            FieldName = "Medication: Aspirin",
            SourceValue = "81mg",
            ConflictingValue = "100mg",
            ResolutionStatus = ConflictResolutionStatus.Open,
            Severity = ConflictSeverity.Warning,
        };

        db.DataConflicts.Add(conflict);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.ResolveConflictAsync(
            new ResolveConflictRequest(conflict.Id, ResolutionAction.AcceptA, null, "Prefer lower dose"),
            userId: Guid.NewGuid(),
            userName: "Nurse A",
            ipAddress: "127.0.0.1",
            cancellationToken: default);

        Assert.True(result.IsSuccess);
        Assert.Equal("81mg", result.Value!.ResolvedValue);

        var updatedConflict = await db.DataConflicts.SingleAsync();
        Assert.Equal(ConflictResolutionStatus.Resolved, updatedConflict.ResolutionStatus);
        Assert.NotNull(updatedConflict.ResolvedAt);
        Assert.Equal("Prefer lower dose", updatedConflict.StaffNotes);

        var audit = await db.AuditLogs.SingleAsync();
        Assert.Equal("DataConflict", audit.Resource);
        Assert.Contains("ConflictResolution", audit.Action);

        _cache.Verify(c => c.RemoveAsync($"patient360:{patientId}", default), Times.Once);
    }

    [Fact]
    public async Task ResolveConflictAsync_AlreadyResolved_ReturnsOptimisticConflictFailure()
    {
        await using var db = CreateDbContext();

        var conflict = new DataConflict
        {
            PatientId = Guid.NewGuid(),
            FieldName = "Allergy: Penicillin",
            SourceValue = "Severe",
            ConflictingValue = "None",
            ResolutionStatus = ConflictResolutionStatus.Resolved,
            Severity = ConflictSeverity.Critical,
            ResolvedAt = DateTime.UtcNow,
        };

        db.DataConflicts.Add(conflict);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.ResolveConflictAsync(
            new ResolveConflictRequest(conflict.Id, ResolutionAction.AcceptB, null, null),
            userId: Guid.NewGuid(),
            userName: "Doctor B",
            ipAddress: null,
            cancellationToken: default);

        Assert.False(result.IsSuccess);
        Assert.StartsWith("CONFLICT_ALREADY_RESOLVED", result.Error);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task GetConflictDetailAsync_WhenConflictExists_ReturnsDetailedResponse()
    {
        await using var db = CreateDbContext();

        var patientId = Guid.NewGuid();
        var sourceDoc = new ClinicalDocument
        {
            PatientId = patientId,
            FileName = "doc-a.pdf",
            EncryptedFilePath = "enc/a",
            ContentType = "application/pdf",
            FileSizeBytes = 100,
        };
        var conflictingDoc = new ClinicalDocument
        {
            PatientId = patientId,
            FileName = "doc-b.pdf",
            EncryptedFilePath = "enc/b",
            ContentType = "application/pdf",
            FileSizeBytes = 120,
        };

        db.ClinicalDocuments.AddRange(sourceDoc, conflictingDoc);
        await db.SaveChangesAsync();

        var conflict = new DataConflict
        {
            PatientId = patientId,
            SourceDocumentId = sourceDoc.Id,
            ConflictingDocumentId = conflictingDoc.Id,
            FieldName = "Medication: Aspirin",
            SourceValue = "81mg",
            ConflictingValue = "100mg",
            Severity = ConflictSeverity.Warning,
            ResolutionStatus = ConflictResolutionStatus.Open,
        };

        db.DataConflicts.Add(conflict);
        db.ExtractedData.AddRange(
            new ExtractedData
            {
                DocumentId = sourceDoc.Id,
                PatientId = patientId,
                Category = DataCategory.Medication,
                Key = "Dose",
                Value = "81mg",
                ConfidenceScore = 0.92,
            },
            new ExtractedData
            {
                DocumentId = conflictingDoc.Id,
                PatientId = patientId,
                Category = DataCategory.Medication,
                Key = "Dose",
                Value = "100mg",
                ConfidenceScore = 0.77,
            });

        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetConflictDetailAsync(conflict.Id, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(conflict.Id, result.Value!.ConflictId);
        Assert.Equal("medication", result.Value.Category);
        Assert.Equal("doc-a.pdf", result.Value.SourceA.DocumentName);
        Assert.Equal("doc-b.pdf", result.Value.SourceB.DocumentName);
        Assert.Equal(0.92, result.Value.SourceA.Confidence, 2);
        Assert.Equal(0.77, result.Value.SourceB.Confidence, 2);
    }

    private ConflictResolutionService CreateSut(ClinicalDbContext dbContext)
    {
        return new ConflictResolutionService(dbContext, _cache.Object, _logger.Object);
    }

    private static ClinicalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ClinicalDbContext>()
            .UseInMemoryDatabase($"conflict-resolution-tests-{Guid.NewGuid()}")
            .Options;

        return new ClinicalDbContext(options);
    }
}
