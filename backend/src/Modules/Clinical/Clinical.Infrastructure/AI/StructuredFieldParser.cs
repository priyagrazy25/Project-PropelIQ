using System.Text.Json;
using Clinical.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Parses AI response text into structured intake fields (AIR-003, AIR-Q03).
/// Validates JSON schema and extracts typed fields with confidence scores.
/// </summary>
public sealed class StructuredFieldParser
{
    private static readonly HashSet<string> ValidCategories =
        ["MedicalHistory", "Symptoms", "Allergies", "Medications"];

    private readonly ILogger<StructuredFieldParser> _logger;

    public StructuredFieldParser(ILogger<StructuredFieldParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses AI response JSON into a conversational message and structured fields.
    /// Returns raw text as message if JSON parsing fails.
    /// </summary>
    public ParseResult Parse(string aiResponseText)
    {
        if (string.IsNullOrWhiteSpace(aiResponseText))
        {
            return new ParseResult(string.Empty, []);
        }

        // Try to extract JSON from the response (AI may include preamble text)
        var jsonText = ExtractJsonBlock(aiResponseText);

        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            var message = root.TryGetProperty("message", out var msgProp)
                ? msgProp.GetString() ?? aiResponseText
                : aiResponseText;

            var fields = new List<ParsedFieldDto>();
            if (root.TryGetProperty("fields", out var fieldsProp) &&
                fieldsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var field in fieldsProp.EnumerateArray())
                {
                    var parsed = ParseField(field);
                    if (parsed is not null)
                    {
                        fields.Add(parsed);
                    }
                }
            }

            return new ParseResult(message, fields);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "AI response is not valid JSON. Treating as plain text message.");
            return new ParseResult(aiResponseText, []);
        }
    }

    private ParsedFieldDto? ParseField(JsonElement field)
    {
        var fieldName = field.TryGetProperty("fieldName", out var fn)
            ? fn.GetString() : null;

        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return null;
        }

        var value = field.TryGetProperty("value", out var v)
            ? v.GetString() ?? string.Empty : string.Empty;

        var confidence = field.TryGetProperty("confidenceScore", out var cs)
            ? ClampConfidence(cs.GetDouble()) : 0.5;

        var category = field.TryGetProperty("category", out var cat)
            ? cat.GetString() ?? "Unknown" : "Unknown";

        // Normalize category to valid values
        if (!ValidCategories.Contains(category))
        {
            category = "Unknown";
        }

        return new ParsedFieldDto(fieldName, value, confidence, category);
    }

    /// <summary>
    /// Extracts the first JSON object from text that may contain non-JSON preamble.
    /// </summary>
    private static string ExtractJsonBlock(string text)
    {
        var startIndex = text.IndexOf('{');
        if (startIndex < 0)
        {
            return text;
        }

        var braceCount = 0;
        for (var i = startIndex; i < text.Length; i++)
        {
            if (text[i] == '{') braceCount++;
            else if (text[i] == '}') braceCount--;

            if (braceCount == 0)
            {
                return text.Substring(startIndex, i - startIndex + 1);
            }
        }

        return text[startIndex..];
    }

    private static double ClampConfidence(double value) =>
        Math.Clamp(value, 0.0, 1.0);
}

/// <summary>
/// Result of parsing an AI response into message and structured fields.
/// </summary>
public sealed record ParseResult(string Message, List<ParsedFieldDto> Fields);
