using Clinical.Application.Abstractions;
using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Clinical.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;
using System.Reflection;

namespace UnitTests.Clinical;

public sealed class CodeVerificationServiceTests
{
    [Fact]
    public async Task VerifyCodeAsync_Accept_SetsVerifiedStatusAndCreatesAuditLog()
    {
        await using var db = CreateDbContext();
        var code = CreateMedicalCode();
        db.MedicalCodes.Add(code);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.VerifyCodeAsync(
            code.Id,
            Guid.NewGuid(),
            new VerificationActionRequest { Action = VerificationAction.Accept },
            cancellationToken: default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Verified", result.Value!.Status);

        var updated = await db.MedicalCodes.SingleAsync();
        Assert.Equal(VerificationStatus.Verified, updated.VerificationStatus);
        Assert.NotNull(updated.VerifiedAt);

        var audit = await db.AuditLogs.SingleAsync();
        Assert.Equal("MedicalCode", audit.Resource);
        Assert.Equal($"CodeVerification.{VerificationAction.Accept}", audit.Action);
    }

    [Fact]
    public async Task VerifyCodeAsync_RejectWithoutReason_ReturnsValidationFailure()
    {
        await using var db = CreateDbContext();
        var code = CreateMedicalCode();
        db.MedicalCodes.Add(code);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.VerifyCodeAsync(
            code.Id,
            Guid.NewGuid(),
            new VerificationActionRequest { Action = VerificationAction.Reject },
            cancellationToken: default);

        Assert.False(result.IsSuccess);
        Assert.Equal("Rejection reason is required when rejecting a code.", result.Error);

        var unchanged = await db.MedicalCodes.SingleAsync();
        Assert.Equal(VerificationStatus.Pending, unchanged.VerificationStatus);
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task VerifyCodeAsync_Override_PreservesOriginalAiSuggestion()
    {
        await using var db = CreateDbContext();
        var code = CreateMedicalCode();
        db.MedicalCodes.Add(code);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.VerifyCodeAsync(
            code.Id,
            Guid.NewGuid(),
            new VerificationActionRequest
            {
                Action = VerificationAction.Override,
                OverrideCode = "E11.9",
                OverrideDescription = "Type 2 diabetes mellitus without complications",
                OverrideReason = "clinical_judgment",
                Notes = "Adjusted based on chart context",
            },
            cancellationToken: default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.WasOverridden);
        Assert.Equal("E11.9", result.Value.Code);
        Assert.Equal("E11.65", result.Value.OriginalAiCode);

        var updated = await db.MedicalCodes.SingleAsync();
        Assert.True(updated.IsOverridden);
        Assert.Equal("E11.65", updated.OriginalAiCode);
        Assert.Equal("Type 2 diabetes mellitus with hyperglycemia", updated.OriginalAiDescription);
        Assert.Equal("E11.9", updated.Code);
        Assert.Equal(VerificationStatus.Overridden, updated.VerificationStatus);
        Assert.Equal("clinical_judgment", updated.OverrideReason);
    }

    [Fact]
    public async Task GetAgreementRateAsync_ComputesExpectedCountsAndRate()
    {
        await using var db = CreateDbContext();
        db.MedicalCodes.AddRange(
            CreateMedicalCode(VerificationStatus.Verified, verifiedAt: DateTime.UtcNow.AddDays(-2)),
            CreateMedicalCode(VerificationStatus.Verified, verifiedAt: DateTime.UtcNow.AddDays(-3)),
            CreateMedicalCode(VerificationStatus.Rejected, verifiedAt: DateTime.UtcNow.AddDays(-1)),
            CreateMedicalCode(VerificationStatus.Overridden, verifiedAt: DateTime.UtcNow.AddDays(-4)),
            CreateMedicalCode(VerificationStatus.Pending),
            CreateMedicalCode(VerificationStatus.NeedsReview));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetAgreementRateAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.AcceptedCount);
        Assert.Equal(1, result.Value.RejectedCount);
        Assert.Equal(1, result.Value.OverriddenCount);
        Assert.Equal(2, result.Value.PendingCount);
        Assert.Equal(4, result.Value.TotalProcessed);
        Assert.Equal(66.67, result.Value.AgreementRate);
        Assert.False(result.Value.TargetMet);
    }

    [Fact]
    public async Task GetVerificationStatisticsAsync_ReturnsDailyCountsAndTopRejectionReasons()
    {
        await using var db = CreateDbContext();
        var now = DateTime.UtcNow;

        db.MedicalCodes.AddRange(
            CreateMedicalCode(
                VerificationStatus.Verified,
                verifiedAt: now.AddDays(-1),
                createdAt: now.AddDays(-1).AddHours(-4)),
            CreateMedicalCode(
                VerificationStatus.Rejected,
                verifiedAt: now.AddDays(-1),
                rejectionReason: "Incorrect mapping",
                createdAt: now.AddDays(-1).AddHours(-2)),
            CreateMedicalCode(
                VerificationStatus.Overridden,
                verifiedAt: now.AddDays(-2),
                createdAt: now.AddDays(-2).AddHours(-1)),
            CreateMedicalCode(
                VerificationStatus.Rejected,
                verifiedAt: now.AddDays(-2),
                rejectionReason: "Incorrect mapping",
                createdAt: now.AddDays(-2).AddHours(-3)));
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetVerificationStatisticsAsync(30);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.DailyCounts.Count);
        Assert.True(result.Value.AverageVerificationTimeHours > 0);

        var topReason = Assert.Single(result.Value.TopRejectionReasons);
        Assert.Equal("Incorrect mapping", topReason.Reason);
        Assert.Equal(2, topReason.Count);
    }

    private static CodeVerificationService CreateSut(ClinicalDbContext dbContext)
    {
        return new CodeVerificationService(
            dbContext,
            new LoggerFactory().CreateLogger<CodeVerificationService>());
    }

    private static ClinicalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ClinicalDbContext>()
            .UseInMemoryDatabase($"code-verification-tests-{Guid.NewGuid()}")
            .Options;

        return new ClinicalDbContext(options);
    }

    private static MedicalCode CreateMedicalCode(
        VerificationStatus verificationStatus = VerificationStatus.Pending,
        DateTime? verifiedAt = null,
        string? rejectionReason = null,
        DateTime? createdAt = null)
    {
        var code = new MedicalCode
        {
            PatientId = Guid.NewGuid(),
            CodeType = MedicalCodeType.ICD10,
            Code = "E11.65",
            Description = "Type 2 diabetes mellitus with hyperglycemia",
            ConfidenceScore = 0.82,
            VerificationStatus = verificationStatus,
            VerifiedAt = verifiedAt,
            RejectionReason = rejectionReason,
        };

        if (createdAt.HasValue)
        {
            SetEntityCreatedAt(code, createdAt.Value);
        }

        return code;
    }

    private static void SetEntityCreatedAt(MedicalCode code, DateTime createdAt)
    {
        var createdAtProperty = typeof(BaseEntity).GetProperty(
            nameof(BaseEntity.CreatedAt),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        createdAtProperty!.SetValue(code, createdAt);
    }
}
