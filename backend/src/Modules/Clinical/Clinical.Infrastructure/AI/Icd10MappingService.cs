using Clinical.Application.Abstractions;
using Clinical.Application.AI;
using Clinical.Domain.Entities;
using Clinical.Domain.Enums;
using Clinical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Domain;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// AI-driven ICD-10-CM mapping service (AIR-004).
/// Uses NER classification via Semantic Kernel + Ollama to extract clinical terms,
/// then matches against ICD-10-CM lookup table to provide top-3 ranked candidates.
/// Enforces 4,096 token budget per request (AIR-O01).
/// </summary>
public sealed partial class Icd10MappingService : IIcd10MappingService
{
    private readonly ClinicalDbContext _dbContext;
    private readonly IAiInferenceService _aiService;
    private readonly TokenBudgetGuard _tokenBudget;
    private readonly ILogger<Icd10MappingService> _logger;

    private const int MaxCandidates = 3;
    private const double LowConfidenceThreshold = 0.5;

    public Icd10MappingService(
        ClinicalDbContext dbContext,
        IAiInferenceService aiService,
        TokenBudgetGuard tokenBudget,
        ILogger<Icd10MappingService> logger)
    {
        _dbContext = dbContext;
        _aiService = aiService;
        _tokenBudget = tokenBudget;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Icd10MappingResult>>> MapDiagnosesAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        // Get diagnoses from extracted data (DataCategory.Diagnosis)
        var diagnoses = await _dbContext.ExtractedData
            .AsNoTracking()
            .Where(e => e.PatientId == patientId && e.Category == DataCategory.Diagnosis)
            .ToListAsync(cancellationToken);

        if (diagnoses.Count == 0)
        {
            _logger.LogInformation("No diagnoses found for patient {PatientId}", patientId);
            return Result<IReadOnlyList<Icd10MappingResult>>.Success(Array.Empty<Icd10MappingResult>());
        }

        // Load ICD-10 lookup table
        var icd10Codes = await _dbContext.Icd10Codes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (icd10Codes.Count == 0)
        {
            _logger.LogWarning("ICD-10 lookup table is empty. Run seed migration.");
            return Result<IReadOnlyList<Icd10MappingResult>>.Failure("ICD-10 lookup table not initialized.");
        }

        var results = new List<Icd10MappingResult>();
        var totalTokensUsed = 0;

        foreach (var diagnosis in diagnoses)
        {
            // Check if already mapped
            var existingCode = await _dbContext.MedicalCodes
                .AsNoTracking()
                .AnyAsync(m => m.ExtractedDataId == diagnosis.Id && m.CodeType == MedicalCodeType.ICD10, cancellationToken);

            if (existingCode)
            {
                _logger.LogDebug("Diagnosis {DiagnosisId} already mapped, skipping", diagnosis.Id);
                continue;
            }

            var mappingResult = await MapSingleDiagnosisAsync(
                diagnosis, icd10Codes, cancellationToken);

            totalTokensUsed += mappingResult.TokensUsed;
            results.Add(mappingResult);

            // Check token budget (AIR-O01: 4,096 per request)
            var budgetCheck = _tokenBudget.Validate(
                string.Join("\n", diagnoses.Select(d => d.Value)),
                AiRequestType.MedicalCoding);

            if (!budgetCheck.WithinBudget)
            {
                _logger.LogWarning(
                    "Token budget exhausted after mapping {Count} diagnoses for patient {PatientId}",
                    results.Count, patientId);
                break;
            }
        }

        // Persist MedicalCode records with "Pending" (Suggested) status (AIR-S04)
        await PersistMappedCodesAsync(patientId, results, cancellationToken);

        _logger.LogInformation(
            "Mapped {DiagnosisCount} diagnoses to ICD-10 codes for patient {PatientId}, {Tokens} tokens used",
            results.Count, patientId, totalTokensUsed);

        return Result<IReadOnlyList<Icd10MappingResult>>.Success(results);
    }

    /// <inheritdoc />
    public async Task<Result<CodeVerificationQueue>> GetVerificationQueueAsync(
        string? statusFilter,
        string? codeTypeFilter,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MedicalCodes
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .AsQueryable();

        // Apply status filter
        if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<VerificationStatus>(statusFilter, true, out var statusEnum))
        {
            query = query.Where(m => m.VerificationStatus == statusEnum);
        }

        // Apply code type filter
        if (!string.IsNullOrEmpty(codeTypeFilter))
        {
            var normalizedType = codeTypeFilter.Replace("-", "");
            if (Enum.TryParse<MedicalCodeType>(normalizedType, true, out var typeEnum))
            {
                query = query.Where(m => m.CodeType == typeEnum);
            }
        }

        var codes = await query
            .Take(100)
            .ToListAsync(cancellationToken);

        var entries = codes.Select(m => new CodeVerificationEntry(
            m.Id,
            m.PatientId,
            "Patient", // Would join with Identity module for actual name
            m.CodeType == MedicalCodeType.ICD10 ? "ICD-10" : "CPT",
            new Icd10Candidate(m.Code, m.Description, m.ConfidenceScore),
            Array.Empty<Icd10Candidate>(),
            m.VerificationStatus.ToString(),
            m.CreatedAt)).ToList();

        var queue = new CodeVerificationQueue(
            entries,
            codes.Count,
            codes.Count(c => c.VerificationStatus == VerificationStatus.Pending));

        return Result<CodeVerificationQueue>.Success(queue);
    }

    /// <inheritdoc />
    public async Task<Result<CodeVerificationResult>> VerifyCodeAsync(
        Guid codeId,
        Guid userId,
        string action,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var code = await _dbContext.MedicalCodes
            .FirstOrDefaultAsync(m => m.Id == codeId, cancellationToken);

        if (code is null)
        {
            return Result<CodeVerificationResult>.Failure("Code entry not found.");
        }

        if (code.VerificationStatus != VerificationStatus.Pending)
        {
            return Result<CodeVerificationResult>.Failure("Code already verified by another user.");
        }

        code.VerificationStatus = action.Equals("accept", StringComparison.OrdinalIgnoreCase)
            ? VerificationStatus.Verified
            : VerificationStatus.Rejected;
        code.VerifiedByUserId = userId == Guid.Empty ? null : userId;
        code.VerifiedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Code {CodeId} ({Code}) {Action} by user {UserId}",
            codeId, code.Code, action, userId);

        return Result<CodeVerificationResult>.Success(new CodeVerificationResult(code.VerificationStatus.ToString()));
    }

    private async Task<Icd10MappingResult> MapSingleDiagnosisAsync(
        ExtractedData diagnosis,
        IReadOnlyList<Icd10Code> icd10Codes,
        CancellationToken cancellationToken)
    {
        var diagnosisText = diagnosis.Value;
        var tokensUsed = 0;

        // Step 1: Use AI to extract clinical terms and classify
        var prompt = BuildClassificationPrompt(diagnosisText);
        var aiResult = await _aiService.GenerateAsync(prompt, AiRequestType.MedicalCoding, cancellationToken);

        if (!aiResult.Success)
        {
            // Fallback to keyword matching only
            _logger.LogWarning("AI classification failed for diagnosis: {Diagnosis}. Using keyword matching.", diagnosisText);
            var keywordCandidates = MatchByKeywords(diagnosisText, icd10Codes);
            return new Icd10MappingResult(
                diagnosis.Id,
                diagnosisText,
                keywordCandidates,
                keywordCandidates.All(c => c.Confidence < LowConfidenceThreshold),
                tokensUsed);
        }

        tokensUsed = aiResult.TokensUsed;

        // Step 2: Parse AI response to extract clinical terms
        var extractedTerms = ParseAiResponse(aiResult.Text);

        // Step 3: Match extracted terms against ICD-10 lookup table
        var candidates = MatchCandidates(extractedTerms, diagnosisText, icd10Codes);

        // Limit to top-3 candidates (AC-2)
        var topCandidates = candidates
            .OrderByDescending(c => c.Confidence)
            .Take(MaxCandidates)
            .ToList();

        var allBelowThreshold = topCandidates.All(c => c.Confidence < LowConfidenceThreshold);

        return new Icd10MappingResult(
            diagnosis.Id,
            diagnosisText,
            topCandidates,
            allBelowThreshold,
            tokensUsed);
    }

    private static string BuildClassificationPrompt(string diagnosisText)
    {
        return $"""
            You are a medical coding assistant. Extract clinical terms from the following diagnosis text.
            Return a JSON array of clinical terms that are relevant for ICD-10-CM coding.
            Focus on: disease names, conditions, anatomical locations, severity indicators, complications.
            
            Diagnosis: {diagnosisText}
            
            Response format (JSON array only, no explanation):
            ["term1", "term2", "term3"]
            """;
    }

    private static IReadOnlyList<string> ParseAiResponse(string aiResponse)
    {
        try
        {
            // Clean response - extract JSON array
            var match = JsonArrayRegex().Match(aiResponse);
            if (!match.Success)
            {
                return Array.Empty<string>();
            }

            var terms = JsonSerializer.Deserialize<string[]>(match.Value);
            return terms ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private IReadOnlyList<Icd10Candidate> MatchCandidates(
        IReadOnlyList<string> extractedTerms,
        string originalDiagnosis,
        IReadOnlyList<Icd10Code> icd10Codes)
    {
        var candidates = new List<(Icd10Code Code, double Score)>();
        var normalizedDiagnosis = NormalizeText(originalDiagnosis);
        var normalizedTerms = extractedTerms.Select(NormalizeText).ToList();

        foreach (var code in icd10Codes)
        {
            var score = CalculateMatchScore(code, normalizedDiagnosis, normalizedTerms);
            if (score > 0)
            {
                candidates.Add((code, score));
            }
        }

        return candidates
            .OrderByDescending(c => c.Score)
            .Take(MaxCandidates)
            .Select(c => new Icd10Candidate(c.Code.Code, c.Code.Description, Math.Round(c.Score, 2)))
            .ToList();
    }

    private IReadOnlyList<Icd10Candidate> MatchByKeywords(
        string diagnosisText,
        IReadOnlyList<Icd10Code> icd10Codes)
    {
        var normalizedDiagnosis = NormalizeText(diagnosisText);
        var candidates = new List<(Icd10Code Code, double Score)>();

        foreach (var code in icd10Codes)
        {
            var score = CalculateKeywordScore(code, normalizedDiagnosis);
            if (score > 0)
            {
                candidates.Add((code, score));
            }
        }

        return candidates
            .OrderByDescending(c => c.Score)
            .Take(MaxCandidates)
            .Select(c => new Icd10Candidate(c.Code.Code, c.Code.Description, Math.Round(c.Score, 2)))
            .ToList();
    }

    private static double CalculateMatchScore(
        Icd10Code code,
        string normalizedDiagnosis,
        IReadOnlyList<string> normalizedTerms)
    {
        var score = 0.0;

        // Check description match
        var normalizedDesc = NormalizeText(code.Description);
        var normalizedShort = NormalizeText(code.ShortDescription);

        // Exact description match
        if (normalizedDiagnosis.Contains(normalizedDesc) || normalizedDesc.Contains(normalizedDiagnosis))
        {
            score += 0.5;
        }

        // Short description match
        if (normalizedDiagnosis.Contains(normalizedShort) || normalizedShort.Contains(normalizedDiagnosis))
        {
            score += 0.3;
        }

        // Keyword matching
        var keywords = code.Keywords.Split('|', StringSplitOptions.RemoveEmptyEntries);
        var matchedKeywords = 0;
        foreach (var keyword in keywords)
        {
            var normalizedKeyword = NormalizeText(keyword);
            if (normalizedDiagnosis.Contains(normalizedKeyword))
            {
                matchedKeywords++;
            }
            if (normalizedTerms.Any(t => t.Contains(normalizedKeyword) || normalizedKeyword.Contains(t)))
            {
                matchedKeywords++;
            }
        }

        if (keywords.Length > 0)
        {
            score += (double)matchedKeywords / (keywords.Length * 2) * 0.4;
        }

        return Math.Min(score, 0.99); // Cap at 0.99
    }

    private static double CalculateKeywordScore(Icd10Code code, string normalizedDiagnosis)
    {
        var keywords = code.Keywords.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (keywords.Length == 0) return 0;

        var matchedCount = keywords.Count(k => normalizedDiagnosis.Contains(NormalizeText(k)));
        var score = (double)matchedCount / keywords.Length;

        // Boost if short description matches
        if (normalizedDiagnosis.Contains(NormalizeText(code.ShortDescription)))
        {
            score = Math.Min(score + 0.3, 0.99);
        }

        return score;
    }

    private static string NormalizeText(string text)
    {
        return text.ToLowerInvariant()
            .Replace(",", " ")
            .Replace(".", " ")
            .Replace("-", " ")
            .Trim();
    }

    private async Task PersistMappedCodesAsync(
        Guid patientId,
        IReadOnlyList<Icd10MappingResult> results,
        CancellationToken cancellationToken)
    {
        var medicalCodes = new List<MedicalCode>();

        foreach (var result in results)
        {
            if (result.Candidates.Count == 0) continue;

            // Store top candidate as primary (others are alternatives in UI)
            var topCandidate = result.Candidates[0];
            var medicalCode = new MedicalCode
            {
                PatientId = patientId,
                ExtractedDataId = result.ExtractedDataId,
                CodeType = MedicalCodeType.ICD10,
                Code = topCandidate.Code,
                Description = topCandidate.Description,
                VerificationStatus = VerificationStatus.Pending // "Suggested" per AIR-S04
            };

            medicalCodes.Add(medicalCode);
        }

        if (medicalCodes.Count > 0)
        {
            await _dbContext.MedicalCodes.AddRangeAsync(medicalCodes, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Persisted {Count} MedicalCode records with Pending status for patient {PatientId}",
                medicalCodes.Count, patientId);
        }
    }

    [GeneratedRegex(@"\[.*?\]", RegexOptions.Singleline)]
    private static partial Regex JsonArrayRegex();
}
