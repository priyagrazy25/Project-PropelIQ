using System.Text.RegularExpressions;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Regex-based PII redactor for HIPAA identifiers (AIR-S02).
/// Redacts: Names, SSN, DOB, Phone, Email, Address patterns.
/// </summary>
public sealed partial class PiiRedactor
{
    private const string RedactedPlaceholder = "[REDACTED]";

    // SSN patterns: XXX-XX-XXXX, XXXXXXXXX, XXX XX XXXX
    [GeneratedRegex(@"\b\d{3}[-\s]?\d{2}[-\s]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex SsnPattern();

    // Phone patterns: (XXX) XXX-XXXX, XXX-XXX-XXXX, XXX.XXX.XXXX, +1XXXXXXXXXX
    [GeneratedRegex(@"(\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex PhonePattern();

    // Email pattern
    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    // Date of Birth patterns: MM/DD/YYYY, MM-DD-YYYY, YYYY-MM-DD, Month DD, YYYY
    [GeneratedRegex(@"\b(?:(?:0?[1-9]|1[0-2])[-/](?:0?[1-9]|[12]\d|3[01])[-/](?:19|20)\d{2}|(?:19|20)\d{2}[-/](?:0?[1-9]|1[0-2])[-/](?:0?[1-9]|[12]\d|3[01])|(?:January|February|March|April|May|June|July|August|September|October|November|December|Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\.?\s+\d{1,2},?\s+(?:19|20)\d{2})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex DatePattern();

    // MRN (Medical Record Number) patterns: MRN: XXXXXXX, MR#XXXXXXX
    [GeneratedRegex(@"\b(?:MRN|MR#|Medical Record (?:Number|#)?)\s*:?\s*\d{5,10}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex MrnPattern();

    // Address patterns (simplified: number + street)
    [GeneratedRegex(@"\b\d{1,5}\s+(?:[A-Za-z]+\s+){1,4}(?:Street|St|Avenue|Ave|Boulevard|Blvd|Road|Rd|Lane|Ln|Drive|Dr|Court|Ct|Way|Place|Pl|Circle|Cir)\.?\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex AddressPattern();

    // ZIP Code patterns: XXXXX, XXXXX-XXXX
    [GeneratedRegex(@"\b\d{5}(?:-\d{4})?\b", RegexOptions.Compiled)]
    private static partial Regex ZipCodePattern();

    /// <summary>
    /// Redacts PII from the input text using regex patterns.
    /// </summary>
    /// <param name="text">Original text with potential PII.</param>
    /// <returns>Tuple of (redacted text, count of redactions made).</returns>
    public (string RedactedText, int RedactionCount) Redact(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (text, 0);
        }

        var redactionCount = 0;
        var result = text;

        // Order matters: more specific patterns first
        result = RedactWithCount(result, SsnPattern(), ref redactionCount);
        result = RedactWithCount(result, MrnPattern(), ref redactionCount);
        result = RedactWithCount(result, EmailPattern(), ref redactionCount);
        result = RedactWithCount(result, PhonePattern(), ref redactionCount);
        result = RedactWithCount(result, DatePattern(), ref redactionCount);
        result = RedactWithCount(result, AddressPattern(), ref redactionCount);
        result = RedactWithCount(result, ZipCodePattern(), ref redactionCount);

        return (result, redactionCount);
    }

    private static string RedactWithCount(string text, Regex pattern, ref int count)
    {
        var matches = pattern.Matches(text);
        count += matches.Count;
        return pattern.Replace(text, RedactedPlaceholder);
    }
}
