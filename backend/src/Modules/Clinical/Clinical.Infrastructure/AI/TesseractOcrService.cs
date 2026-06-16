using Clinical.Application.AI;
using Microsoft.Extensions.Logging;
using Tesseract;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Tesseract OCR 5.x wrapper for clinical document text extraction (AIR-001).
/// </summary>
public sealed class TesseractOcrService : IOcrService, IDisposable
{
    private readonly TesseractEngine _engine;
    private readonly ILogger<TesseractOcrService> _logger;
    private bool _disposed;

    public TesseractOcrService(TesseractOcrOptions options, ILogger<TesseractOcrService> logger)
    {
        _logger = logger;
        _engine = new TesseractEngine(options.TessDataPath, options.Language, EngineMode.Default);
    }

    public Task<OcrResult> ExtractTextAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ExtractText(imageData), cancellationToken);
    }

    public Task<OcrResult> ExtractTextFromPdfPageAsync(byte[] pdfPageImage, CancellationToken cancellationToken = default)
    {
        return ExtractTextAsync(pdfPageImage, cancellationToken);
    }

    private OcrResult ExtractText(byte[] imageData)
    {
        try
        {
            using var pix = Pix.LoadFromMemory(imageData);
            using var page = _engine.Process(pix);

            var text = page.GetText();
            var confidence = page.GetMeanConfidence();

            return new OcrResult(text, confidence, Success: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tesseract OCR extraction failed.");
            return new OcrResult(string.Empty, 0f, Success: false, Error: ex.Message);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _engine.Dispose();
            _disposed = true;
        }
    }
}

public sealed class TesseractOcrOptions
{
    public const string SectionName = "TesseractOcr";

    public string TessDataPath { get; set; } = "./tessdata";
    public string Language { get; set; } = "eng";
}
