using Clinical.Domain.Entities;
using Clinical.Domain.Enums;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Semantic de-duplicator for merging equivalent extracted data entries (AIR-002).
/// Handles brand/generic medication matching and near-duplicate field consolidation.
/// </summary>
public sealed class SemanticDeduplicator
{
    /// <summary>
    /// Known brand-to-generic medication mappings for semantic matching.
    /// In production, this would be backed by RxNorm or similar terminology service.
    /// </summary>
    private static readonly Dictionary<string, string> BrandToGenericMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Common medication mappings
        ["Tylenol"] = "acetaminophen",
        ["Advil"] = "ibuprofen",
        ["Motrin"] = "ibuprofen",
        ["Lipitor"] = "atorvastatin",
        ["Zocor"] = "simvastatin",
        ["Crestor"] = "rosuvastatin",
        ["Glucophage"] = "metformin",
        ["Synthroid"] = "levothyroxine",
        ["Levoxyl"] = "levothyroxine",
        ["Norvasc"] = "amlodipine",
        ["Zestril"] = "lisinopril",
        ["Prinivil"] = "lisinopril",
        ["Lopressor"] = "metoprolol",
        ["Toprol"] = "metoprolol",
        ["Xanax"] = "alprazolam",
        ["Valium"] = "diazepam",
        ["Prilosec"] = "omeprazole",
        ["Nexium"] = "esomeprazole",
        ["Protonix"] = "pantoprazole",
        ["Lasix"] = "furosemide",
        ["Coumadin"] = "warfarin",
        ["Plavix"] = "clopidogrel",
        ["Cardizem"] = "diltiazem",
        ["Lanoxin"] = "digoxin",
        ["Tenormin"] = "atenolol",
        ["Prozac"] = "fluoxetine",
        ["Zoloft"] = "sertraline",
        ["Lexapro"] = "escitalopram",
        ["Celexa"] = "citalopram",
        ["Effexor"] = "venlafaxine",
        ["Cymbalta"] = "duloxetine",
        ["Wellbutrin"] = "bupropion",
        ["Ambien"] = "zolpidem",
        ["Lunesta"] = "eszopiclone"
    };

    /// <summary>
    /// De-duplicates extracted data by merging semantically equivalent entries.
    /// Keeps the entry with highest confidence score when duplicates are found.
    /// </summary>
    /// <param name="data">List of extracted data to de-duplicate.</param>
    /// <returns>De-duplicated list with merged entries.</returns>
    public IReadOnlyList<ExtractedData> Deduplicate(IReadOnlyList<ExtractedData> data)
    {
        if (data.Count <= 1)
        {
            return data;
        }

        // Group by category first
        var grouped = data.GroupBy(d => d.Category);
        var result = new List<ExtractedData>();

        foreach (var categoryGroup in grouped)
        {
            var categoryData = categoryGroup.ToList();

            switch (categoryGroup.Key)
            {
                case DataCategory.Medication:
                    result.AddRange(DeduplicateMedications(categoryData));
                    break;

                case DataCategory.Allergy:
                    result.AddRange(DeduplicateByNormalizedKey(categoryData));
                    break;

                case DataCategory.Diagnosis:
                    result.AddRange(DeduplicateDiagnoses(categoryData));
                    break;

                default:
                    result.AddRange(DeduplicateByNormalizedKey(categoryData));
                    break;
            }
        }

        return result.OrderByDescending(d => d.ConfidenceScore).ToList();
    }

    /// <summary>
    /// De-duplicates medications by matching brand/generic equivalents.
    /// </summary>
    private IReadOnlyList<ExtractedData> DeduplicateMedications(List<ExtractedData> medications)
    {
        var seen = new Dictionary<string, ExtractedData>(StringComparer.OrdinalIgnoreCase);

        foreach (var med in medications.OrderByDescending(m => m.ConfidenceScore))
        {
            var normalizedKey = NormalizeMedicationName(med.Key);

            if (seen.TryGetValue(normalizedKey, out var existing))
            {
                // Keep higher confidence, but merge source info if different documents
                if (med.ConfidenceScore > existing.ConfidenceScore)
                {
                    seen[normalizedKey] = med;
                }
            }
            else
            {
                seen[normalizedKey] = med;
            }
        }

        return seen.Values.ToList();
    }

    /// <summary>
    /// De-duplicates diagnoses by ICD code or normalized name.
    /// </summary>
    private IReadOnlyList<ExtractedData> DeduplicateDiagnoses(List<ExtractedData> diagnoses)
    {
        var seen = new Dictionary<string, ExtractedData>(StringComparer.OrdinalIgnoreCase);

        foreach (var diag in diagnoses.OrderByDescending(d => d.ConfidenceScore))
        {
            // Prefer ICD code if present, otherwise use normalized key
            var normalizedKey = ExtractIcdCode(diag.Value) ?? NormalizeKey(diag.Key);

            if (seen.TryGetValue(normalizedKey, out var existing))
            {
                if (diag.ConfidenceScore > existing.ConfidenceScore)
                {
                    seen[normalizedKey] = diag;
                }
            }
            else
            {
                seen[normalizedKey] = diag;
            }
        }

        return seen.Values.ToList();
    }

    /// <summary>
    /// Generic de-duplication using normalized key matching.
    /// </summary>
    private IReadOnlyList<ExtractedData> DeduplicateByNormalizedKey(List<ExtractedData> data)
    {
        var seen = new Dictionary<string, ExtractedData>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in data.OrderByDescending(d => d.ConfidenceScore))
        {
            var normalizedKey = NormalizeKey(item.Key);

            if (seen.TryGetValue(normalizedKey, out var existing))
            {
                if (item.ConfidenceScore > existing.ConfidenceScore)
                {
                    seen[normalizedKey] = item;
                }
            }
            else
            {
                seen[normalizedKey] = item;
            }
        }

        return seen.Values.ToList();
    }

    /// <summary>
    /// Normalizes medication name by converting brand names to generic.
    /// </summary>
    private static string NormalizeMedicationName(string name)
    {
        var trimmed = name.Trim();

        // Check direct match first
        if (BrandToGenericMap.TryGetValue(trimmed, out var generic))
        {
            return generic.ToLowerInvariant();
        }

        // Check if name contains a brand name
        foreach (var (brand, genericName) in BrandToGenericMap)
        {
            if (trimmed.Contains(brand, StringComparison.OrdinalIgnoreCase))
            {
                return genericName.ToLowerInvariant();
            }
        }

        return NormalizeKey(trimmed);
    }

    /// <summary>
    /// Extracts ICD-10 code from diagnosis value if present.
    /// </summary>
    private static string? ExtractIcdCode(string value)
    {
        // ICD-10 codes are alphanumeric, e.g., "E11.9" or "I10"
        var icdPattern = System.Text.RegularExpressions.Regex.Match(
            value,
            @"\b([A-TV-Z]\d{2}(?:\.\d{1,4})?)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return icdPattern.Success ? icdPattern.Groups[1].Value.ToUpperInvariant() : null;
    }

    /// <summary>
    /// Normalizes key for comparison: lowercase, trimmed, no extra spaces.
    /// </summary>
    private static string NormalizeKey(string key)
    {
        return System.Text.RegularExpressions.Regex
            .Replace(key.ToLowerInvariant().Trim(), @"\s+", " ");
    }
}
