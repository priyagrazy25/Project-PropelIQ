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
/// AI-driven CPT code mapping service (AIR-005).
/// Uses NER classification via Semantic Kernel + Ollama to extract procedure terms,
/// then matches against CPT reference table to provide top-3 ranked candidates.
/// Enforces 4,096 token budget per request (AIR-O01).
/// </summary>
public sealed partial class CptMappingService : ICptMappingService
{
    private readonly ClinicalDbContext _dbContext;
    private readonly IAiInferenceService _aiService;
    private readonly TokenBudgetGuard _tokenBudget;
    private readonly ILogger<CptMappingService> _logger;

    private const int MaxCandidates = 3;
    private const double LowConfidenceThreshold = 0.5;

    public CptMappingService(
        ClinicalDbContext dbContext,
        IAiInferenceService aiService,
        TokenBudgetGuard tokenBudget,
        ILogger<CptMappingService> logger)
    {
        _dbContext = dbContext;
        _aiService = aiService;
        _tokenBudget = tokenBudget;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CptMappingResult>>> MapProceduresAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        // Get procedures from extracted data (DataCategory.Procedure)
        var procedures = await _dbContext.ExtractedData
            .AsNoTracking()
            .Where(e => e.PatientId == patientId && e.Category == DataCategory.Procedure)
            .ToListAsync(cancellationToken);

        if (procedures.Count == 0)
        {
            _logger.LogInformation("No procedures found for patient {PatientId}", patientId);
            return Result<IReadOnlyList<CptMappingResult>>.Success(Array.Empty<CptMappingResult>());
        }

        // Load CPT lookup table
        var cptCodes = await _dbContext.CptCodes
            .AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);

        if (cptCodes.Count == 0)
        {
            _logger.LogWarning("CPT lookup table is empty. Run seed migration.");
            return Result<IReadOnlyList<CptMappingResult>>.Failure("CPT lookup table not initialized.");
        }

        var results = new List<CptMappingResult>();
        var totalTokensUsed = 0;

        foreach (var procedure in procedures)
        {
            // Check if already mapped
            var existingCode = await _dbContext.MedicalCodes
                .AsNoTracking()
                .AnyAsync(m => m.ExtractedDataId == procedure.Id && m.CodeType == MedicalCodeType.CPT, cancellationToken);

            if (existingCode)
            {
                _logger.LogDebug("Procedure {ProcedureId} already mapped, skipping", procedure.Id);
                continue;
            }

            var mappingResult = await MapSingleProcedureAsync(
                procedure, cptCodes, cancellationToken);

            totalTokensUsed += mappingResult.TokensUsed;
            results.Add(mappingResult);

            // Check token budget (AIR-O01: 4,096 per request)
            var budgetCheck = _tokenBudget.Validate(
                string.Join("\n", procedures.Select(p => p.Value)),
                AiRequestType.MedicalCoding);

            if (!budgetCheck.WithinBudget)
            {
                _logger.LogWarning(
                    "Token budget exhausted after mapping {Count} procedures for patient {PatientId}",
                    results.Count, patientId);
                break;
            }
        }

        // Persist MedicalCode records with "Pending" (Suggested) status (AIR-S04)
        await PersistMappedCodesAsync(patientId, results, cancellationToken);

        _logger.LogInformation(
            "Mapped {ProcedureCount} procedures to CPT codes for patient {PatientId}, {Tokens} tokens used",
            results.Count, patientId, totalTokensUsed);

        return Result<IReadOnlyList<CptMappingResult>>.Success(results);
    }

    private async Task<CptMappingResult> MapSingleProcedureAsync(
        ExtractedData procedure,
        IReadOnlyList<CptCode> cptCodes,
        CancellationToken cancellationToken)
    {
        var procedureText = procedure.Value;
        var tokensUsed = 0;

        // Step 1: Use AI to extract procedure terms and classify
        var prompt = BuildClassificationPrompt(procedureText);
        var aiResult = await _aiService.GenerateAsync(prompt, AiRequestType.MedicalCoding, cancellationToken);

        if (!aiResult.Success)
        {
            // Fallback to keyword matching only
            _logger.LogWarning("AI classification failed for procedure: {Procedure}. Using keyword matching.", procedureText);
            var keywordCandidates = MatchByKeywords(procedureText, cptCodes);
            return new CptMappingResult(
                procedure.Id,
                procedureText,
                keywordCandidates,
                keywordCandidates.All(c => c.Confidence < LowConfidenceThreshold),
                tokensUsed);
        }

        tokensUsed = aiResult.TokensUsed;

        // Step 2: Parse AI response to extract procedure terms
        var extractedTerms = ParseAiResponse(aiResult.Text);

        // Step 3: Match extracted terms against CPT lookup table
        var candidates = MatchCandidates(extractedTerms, procedureText, cptCodes);

        // Limit to top-3 candidates (AC-2)
        var topCandidates = candidates
            .OrderByDescending(c => c.Confidence)
            .Take(MaxCandidates)
            .ToList();

        var allBelowThreshold = topCandidates.All(c => c.Confidence < LowConfidenceThreshold);

        return new CptMappingResult(
            procedure.Id,
            procedureText,
            topCandidates,
            allBelowThreshold,
            tokensUsed);
    }

    private static string BuildClassificationPrompt(string procedureText)
    {
        return $"""
            You are a medical coding assistant. Extract procedure terms from the following text.
            Return a JSON array of procedure terms that are relevant for CPT coding.
            Focus on: procedure names, anatomical locations, methods, approach, complexity indicators.
            
            Procedure: {procedureText}
            
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

    private IReadOnlyList<CptCandidate> MatchCandidates(
        IReadOnlyList<string> extractedTerms,
        string originalProcedure,
        IReadOnlyList<CptCode> cptCodes)
    {
        var candidates = new List<(CptCode Code, double Score)>();
        var normalizedProcedure = NormalizeText(originalProcedure);
        var normalizedTerms = extractedTerms.Select(NormalizeText).ToList();

        foreach (var code in cptCodes)
        {
            var score = CalculateMatchScore(code, normalizedProcedure, normalizedTerms);
            if (score > 0)
            {
                candidates.Add((code, score));
            }
        }

        return candidates
            .OrderByDescending(c => c.Score)
            .Take(MaxCandidates)
            .Select(c => new CptCandidate(c.Code.Code, c.Code.Description, Math.Round(c.Score, 2)))
            .ToList();
    }

    private IReadOnlyList<CptCandidate> MatchByKeywords(
        string procedureText,
        IReadOnlyList<CptCode> cptCodes)
    {
        var normalizedProcedure = NormalizeText(procedureText);
        var candidates = new List<(CptCode Code, double Score)>();

        foreach (var code in cptCodes)
        {
            var score = CalculateKeywordScore(code, normalizedProcedure);
            if (score > 0)
            {
                candidates.Add((code, score));
            }
        }

        return candidates
            .OrderByDescending(c => c.Score)
            .Take(MaxCandidates)
            .Select(c => new CptCandidate(c.Code.Code, c.Code.Description, Math.Round(c.Score, 2)))
            .ToList();
    }

    private static double CalculateMatchScore(
        CptCode code,
        string normalizedProcedure,
        IReadOnlyList<string> normalizedTerms)
    {
        var score = 0.0;

        // Check description match
        var normalizedDesc = NormalizeText(code.Description);
        var normalizedShort = NormalizeText(code.ShortDescription);

        // Exact description match
        if (normalizedProcedure.Contains(normalizedDesc) || normalizedDesc.Contains(normalizedProcedure))
        {
            score += 0.5;
        }

        // Short description match
        if (normalizedProcedure.Contains(normalizedShort) || normalizedShort.Contains(normalizedProcedure))
        {
            score += 0.3;
        }

        // Keyword matching
        var keywords = code.Keywords.Split('|', StringSplitOptions.RemoveEmptyEntries);
        var matchedKeywords = 0;
        foreach (var keyword in keywords)
        {
            var normalizedKeyword = NormalizeText(keyword);
            if (normalizedProcedure.Contains(normalizedKeyword))
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

    private static double CalculateKeywordScore(CptCode code, string normalizedProcedure)
    {
        var keywords = code.Keywords.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (keywords.Length == 0) return 0;

        var matchedCount = keywords.Count(k => normalizedProcedure.Contains(NormalizeText(k)));
        var score = (double)matchedCount / keywords.Length;

        // Boost if short description matches
        if (normalizedProcedure.Contains(NormalizeText(code.ShortDescription)))
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
        IReadOnlyList<CptMappingResult> results,
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
                CodeType = MedicalCodeType.CPT,
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
                "Persisted {Count} CPT MedicalCode records with Pending status for patient {PatientId}",
                medicalCodes.Count, patientId);
        }
    }

    [GeneratedRegex(@"\[.*?\]", RegexOptions.Singleline)]
    private static partial Regex JsonArrayRegex();
}
