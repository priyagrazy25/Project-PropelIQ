using Clinical.Application.Models;

namespace Clinical.Infrastructure.AI;

/// <summary>
/// Semantic Kernel prompt template for medical intake conversation (AIR-003).
/// Enforces English-only responses and structured JSON output with confidence scoring.
/// </summary>
public static class IntakePromptTemplate
{
    private const string SystemInstruction = """
        You are a medical intake assistant. Respond ONLY in English.
        Collect: medical history, symptoms, allergies, medications.
        Ask ONE question at a time. Never give medical advice.

        Respond in this JSON format:
        {"message":"your response","fields":[{"fieldName":"Name","value":"val","confidenceScore":0.9,"category":"MedicalHistory|Symptoms|Allergies|Medications"}]}
        Return empty fields array if nothing to extract.
        """;

    /// <summary>
    /// Builds the full prompt including system instructions and conversation history.
    /// </summary>
    public static string Build(IntakeSessionState session, string patientMessage)
    {
        // Keep only the last 6 turns to stay within token budget for phi3:mini
        var recentHistory = session.ConversationHistory
            .TakeLast(6)
            .Select(t => $"{t.Role}: {t.Content}");
        var conversationContext = string.Join("\n", recentHistory);

        return $"""
            {SystemInstruction}

            Conversation so far:
            {conversationContext}
            user: {patientMessage}

            Extract any structured data from the patient's latest message and respond with the next question.
            """;
    }

    /// <summary>
    /// Builds a graceful summary prompt when token budget is nearly exhausted.
    /// </summary>
    public static string BuildSummaryPrompt(IntakeSessionState session)
    {
        var fields = string.Join("\n",
            session.ParsedFields.Select(f => $"- {f.FieldName}: {f.Value} (confidence: {f.ConfidenceScore:F2})"));

        return $"""
            {SystemInstruction}

            The conversation token budget is nearly exhausted. Summarize the collected information and ask the patient to review.

            Collected fields:
            {fields}

            Respond with a summary message and the same fields in JSON format.
            """;
    }
}
