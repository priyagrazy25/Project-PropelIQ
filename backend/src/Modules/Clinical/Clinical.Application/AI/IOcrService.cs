namespace Clinical.Application.AI;

/// <summary>
/// Abstraction for OCR text extraction from scanned documents (AIR-001).
/// </summary>
public interface IOcrService
{
    Task<OcrResult> ExtractTextAsync(byte[] imageData, CancellationToken cancellationToken = default);

    Task<OcrResult> ExtractTextFromPdfPageAsync(byte[] pdfPageImage, CancellationToken cancellationToken = default);
}

public sealed record OcrResult(string Text, float Confidence, bool Success, string? Error = null);
