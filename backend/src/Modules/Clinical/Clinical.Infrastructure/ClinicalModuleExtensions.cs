using Clinical.Application.Abstractions;
using Clinical.Application.AI;
using Clinical.Application.Validators;
using Clinical.Infrastructure.AI;
using Clinical.Infrastructure.Data;
using Clinical.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Security.Authentication;

namespace Clinical.Infrastructure;

public static class ClinicalModuleExtensions
{
    public static IServiceCollection AddClinicalModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ClinicalDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("ClinicalDb"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "clinical");
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                }));

        // AI services (Ollama + Semantic Kernel)
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));
        var ollamaOptions = new OllamaOptions();
        configuration.GetSection(OllamaOptions.SectionName).Bind(ollamaOptions);

        services.AddSingleton<AiCircuitBreaker>();
        services.AddSingleton<TokenBudgetGuard>();
        services.AddSingleton<ModelVersionManager>();
        services.AddSingleton(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return SemanticKernelConfig.CreateKernel(ollamaOptions, loggerFactory);
        });
        services.AddSingleton<IAiInferenceService, OllamaService>();

        // Conversational intake engine (AIR-003, AIR-Q02, AIR-008)
        services.AddSingleton<StructuredFieldParser>();
        services.AddSingleton<ConfidenceScorer>();
        services.AddScoped<IConversationalIntakeEngine, ConversationalIntakeEngine>();

        // Tesseract OCR
        var ocrOptions = new TesseractOcrOptions();
        configuration.GetSection(TesseractOcrOptions.SectionName).Bind(ocrOptions);
        services.AddSingleton(ocrOptions);
        services.AddSingleton<IOcrService, TesseractOcrService>();

        // scispaCy NER HTTP client
        services.Configure<NerServiceOptions>(configuration.GetSection(NerServiceOptions.SectionName));
        var nerOptions = new NerServiceOptions();
        configuration.GetSection(NerServiceOptions.SectionName).Bind(nerOptions);

        services.AddHttpClient<INerService, ScispaCyNerService>(client =>
        {
            client.BaseAddress = new Uri(nerOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(() => CreateTls12Handler());

        // Intake session service
        services.AddScoped<IIntakeSessionService, IntakeSessionService>();

        // Manual intake service (TASK_002)
        services.AddScoped<ManualIntakeValidator>();
        services.AddScoped<IManualIntakeService, ManualIntakeService>();

        // Insurance validation service (TASK_002_BE_INSURANCE_PRECHECK_API)
        services.AddScoped<IInsuranceValidationService, InsuranceValidationService>();

        // Document storage service (TASK_002_BE_DOCUMENT_UPLOAD_API, DR-004)
        services.Configure<DocumentStorageOptions>(configuration.GetSection(DocumentStorageOptions.SectionName));
        services.AddScoped<IDocumentStorageService, DocumentStorageService>();
        services.AddScoped<IDocumentUploadService, DocumentUploadService>();

        // File encryption service with AES-256-GCM (DR-004, NFR-005)
        services.AddSingleton<IFileEncryptionService, FileEncryptionService>();

        // OCR extraction pipeline (AIR-001)
        services.AddSingleton<PiiRedactor>();
        services.AddSingleton(new TextChunker(chunkSize: 512, overlap: 51));
        services.AddScoped<IOcrExtractionPipeline, OcrExtractionPipeline>();

        // NER extraction pipeline (AIR-001, AIR-Q03, AIR-Q04, DR-010)
        services.AddScoped<INerExtractionPipeline, NerExtractionPipeline>();

        // Embedding service with circuit breaker (AIR-R04, AIR-O02)
        services.AddSingleton<EmbeddingCircuitBreaker>();
        services.AddHttpClient<IEmbeddingService, EmbeddingService>(client =>
        {
            client.BaseAddress = new Uri(ollamaOptions.Endpoint);
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .ConfigurePrimaryHttpMessageHandler(() => CreateTls12Handler());

        // Extraction job queue with SemaphoreSlim(2) concurrency (AIR-O04)
        services.AddSingleton<IExtractionJobQueue, ExtractionJobQueue>();

        // Processing metrics tracker (NFR-016: 10,000 docs/month)
        services.AddSingleton<ExtractionMetricsTracker>();

        // 360-degree patient view (SCR-016, AIR-002, NFR-004, NFR-017)
        services.AddSingleton<SemanticDeduplicator>();
        services.AddSingleton<SeverityClassifier>();
        services.AddScoped<IConflictDetectionService, ConflictDetectionService>();
        services.AddScoped<IPatientView360Service, PatientView360Service>();

        // Conflict resolution service (SCR-017, AC-2, AC-3, DR-012)
        services.AddScoped<IConflictResolutionService, ConflictResolutionService>();

        // ICD-10 mapping service (SCR-018, AIR-004, AIR-O01, AIR-S04)
        services.AddScoped<IIcd10MappingService, Icd10MappingService>();

        // CPT mapping service (SCR-018, AIR-005, AIR-O01, AIR-S04)
        services.AddScoped<ICptMappingService, CptMappingService>();

        // Code verification service (AIR-S04, AC-1, AC-2, AC-5)
        services.AddScoped<ICodeVerificationService, CodeVerificationService>();

        // Agreement rate tracker (AIR-Q01: >98% agreement rate target)
        services.AddScoped<AgreementRateTracker>();

        // RAG retrieval pipeline (AIR-R02, AIR-R03)
        services.AddSingleton<HybridReranker>();
        services.AddSingleton<ContextAssembler>();
        services.AddScoped<IRagRetrievalService, RagRetrievalService>();

        // Background workers
        services.AddHostedService<OcrProcessingWorker>();
        services.AddHostedService<NerProcessingWorker>();
        services.AddHostedService<ExtractionJobWorker>();

        services.AddHealthChecks()
            .AddCheck<ClinicalHealthCheck>("clinical_module", tags: ["module"]);

        return services;
    }

    /// <summary>
    /// Creates an HttpMessageHandler that enforces TLS 1.2+ for outbound connections (NFR-006).
    /// </summary>
    private static HttpMessageHandler CreateTls12Handler()
    {
        return new SocketsHttpHandler
        {
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }
        };
    }
}

public sealed class ClinicalHealthCheck : IHealthCheck
{
    private readonly ClinicalDbContext _dbContext;

    public ClinicalHealthCheck(ClinicalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy("Clinical module is operational.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Clinical module database unavailable.", ex);
        }
    }
}
