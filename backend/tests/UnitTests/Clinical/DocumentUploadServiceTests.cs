using Clinical.Application.Abstractions;
using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Clinical.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel.Domain;

namespace UnitTests.Clinical;

public sealed class DocumentUploadServiceTests
{
    private readonly Mock<IDocumentStorageService> _storage = new();
    private readonly Mock<IPatientView360Service> _patientView360 = new();
    private readonly Mock<ILogger<DocumentUploadService>> _logger = new();

    [Fact]
    public async Task UploadDocumentAsync_StorageSucceeds_PersistsQueuedDocumentAndInvalidatesCache()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string fileName = "..\\unsafe/report.pdf";

        _storage
            .Setup(s => s.StoreDocumentAsync(patientId, fileName, "application/pdf", It.IsAny<Stream>(), default))
            .ReturnsAsync(Result<string>.Success("enc/path/report.pdf"));

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var result = await sut.UploadDocumentAsync(
            patientId,
            userId,
            fileName,
            "application/pdf",
            fileSizeBytes: stream.Length,
            stream,
            default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Queued", result.Value!.ProcessingStatus);

        var stored = await db.ClinicalDocuments.SingleAsync();
        Assert.Equal(patientId, stored.PatientId);
        Assert.Equal("unsafe_report.pdf", stored.FileName);
        Assert.Equal("enc/path/report.pdf", stored.EncryptedFilePath);
        Assert.Equal(ProcessingStatus.Pending, stored.ProcessingStatus);

        _patientView360.Verify(v => v.InvalidateCacheAsync(patientId, default), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_StorageFails_ReturnsFailureAndDoesNotPersist()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var patientId = Guid.NewGuid();

        _storage
            .Setup(s => s.StoreDocumentAsync(patientId, "report.pdf", "application/pdf", It.IsAny<Stream>(), default))
            .ReturnsAsync(Result<string>.Failure("storage unavailable"));

        using var stream = new MemoryStream(new byte[] { 1 });

        var result = await sut.UploadDocumentAsync(
            patientId,
            userId: null,
            fileName: "report.pdf",
            contentType: "application/pdf",
            fileSizeBytes: stream.Length,
            stream,
            default);

        Assert.False(result.IsSuccess);
        Assert.Equal("storage unavailable", result.Error);
        Assert.Empty(db.ClinicalDocuments);
        _patientView360.Verify(v => v.InvalidateCacheAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Theory]
    [InlineData(ProcessingStatus.OcrInProgress, "Processing")]
    [InlineData(ProcessingStatus.Completed, "Complete")]
    [InlineData(ProcessingStatus.Failed, "Failed")]
    public async Task GetDocumentStatusAsync_MapsDomainStatusToApiStatus(
        ProcessingStatus processingStatus,
        string expectedStatus)
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var patientId = Guid.NewGuid();
        var doc = new ClinicalDocument
        {
            PatientId = patientId,
            FileName = "report.pdf",
            EncryptedFilePath = "enc/path/report.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 100,
            ProcessingStatus = processingStatus,
        };

        db.ClinicalDocuments.Add(doc);
        await db.SaveChangesAsync();

        var result = await sut.GetDocumentStatusAsync(doc.Id, patientId, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedStatus, result.Value!.ProcessingStatus);
    }

    private DocumentUploadService CreateSut(ClinicalDbContext dbContext)
    {
        return new DocumentUploadService(
            _storage.Object,
            dbContext,
            _patientView360.Object,
            _logger.Object);
    }

    private static ClinicalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ClinicalDbContext>()
            .UseInMemoryDatabase($"doc-upload-tests-{Guid.NewGuid()}")
            .Options;

        return new ClinicalDbContext(options);
    }
}
