using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using SharedKernel.Domain;

namespace UnitTests.Clinical;

public class ClinicalDocumentEntityTests
{
    [Fact]
    public void NewDocument_ShouldDefaultToPendingStatus()
    {
        var doc = new ClinicalDocument();
        Assert.Equal(ProcessingStatus.Pending, doc.ProcessingStatus);
    }

    [Fact]
    public void NewDocument_ShouldHaveEmptyCollections()
    {
        var doc = new ClinicalDocument();
        Assert.Empty(doc.ExtractedDataPoints);
        Assert.Empty(doc.Embeddings);
    }
}

public class ExtractedDataEntityTests
{
    [Fact]
    public void NewExtractedData_ShouldHaveGuidId()
    {
        var data = new ExtractedData();
        Assert.NotEqual(Guid.Empty, data.Id);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void ConfidenceScore_AcceptsValidRange(double score)
    {
        var data = new ExtractedData { ConfidenceScore = score };
        Assert.Equal(score, data.ConfidenceScore);
    }
}

public class PatientView360EntityTests
{
    [Fact]
    public void NewView_ShouldHaveTimestamp()
    {
        var before = DateTime.UtcNow;
        var view = new PatientView360();
        Assert.True(view.LastRefreshedAt >= before);
    }

    [Fact]
    public void Vitals_ShouldBeNullByDefault()
    {
        var view = new PatientView360();
        Assert.Null(view.Vitals);
    }
}

public class DataConflictEntityTests
{
    [Fact]
    public void NewConflict_ShouldDefaultToWarning()
    {
        var conflict = new DataConflict();
        Assert.Equal(ConflictSeverity.Warning, conflict.Severity);
    }

    [Fact]
    public void NewConflict_ShouldDefaultToOpen()
    {
        var conflict = new DataConflict();
        Assert.Equal(ConflictResolutionStatus.Open, conflict.ResolutionStatus);
    }
}

public class MedicalCodeEntityTests
{
    [Fact]
    public void NewCode_ShouldDefaultToPendingVerification()
    {
        var code = new MedicalCode();
        Assert.Equal(VerificationStatus.Pending, code.VerificationStatus);
    }
}

public class AuditLogEntityTests
{
    [Fact]
    public void NewAuditLog_ShouldHaveTimestamp()
    {
        var before = DateTime.UtcNow;
        var log = new AuditLog();
        Assert.True(log.Timestamp >= before);
    }

    [Fact]
    public void NewAuditLog_ShouldHaveGuidId()
    {
        var log = new AuditLog();
        Assert.NotEqual(Guid.Empty, log.Id);
    }

    [Fact]
    public void AuditLog_PropertiesAreInitOnly()
    {
        var log = new AuditLog
        {
            ActorId = Guid.NewGuid(),
            ActorName = "TestUser",
            Action = "Create",
            Resource = "Patient",
            ResourceId = "abc-123"
        };

        Assert.Equal("TestUser", log.ActorName);
        Assert.Equal("Create", log.Action);
        Assert.Equal("Patient", log.Resource);
    }
}

public class NoShowRiskScoreEntityTests
{
    [Fact]
    public void NewScore_ShouldHaveCalculatedAt()
    {
        var before = DateTime.UtcNow;
        var score = new global::Scheduling.Domain.Entities.NoShowRiskScore();
        Assert.True(score.CalculatedAt >= before);
    }
}
