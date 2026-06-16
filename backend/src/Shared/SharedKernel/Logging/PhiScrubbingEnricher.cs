using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace SharedKernel.Logging;

/// <summary>
/// Serilog enricher that detects and redacts Protected Health Information (PHI) patterns
/// from log message templates and property values per NFR-011.
/// Patterns: SSN, email, phone numbers, dates of birth, MRN-like identifiers,
/// insurance policy/group numbers, credit card numbers, and patient names.
/// </summary>
public sealed partial class PhiScrubbingEnricher : ILogEventEnricher
{
    private const string Redacted = "[PHI-REDACTED]";

    // Property names that should always be redacted (audit-related)
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ssn", "socialsecuritynumber", "social_security_number",
        "email", "emailaddress", "email_address",
        "phone", "phonenumber", "phone_number", "contactnumber", "contact_number",
        "dob", "dateofbirth", "date_of_birth", "birthdate", "birth_date",
        "mrn", "medicalrecordnumber", "medical_record_number",
        "insurancepolicynumber", "insurance_policy_number", "policynumber", "policy_number",
        "insurancegroupnumber", "insurance_group_number", "groupnumber", "group_number",
        "creditcard", "credit_card", "cardnumber", "card_number",
        "patientname", "patient_name", "fullname", "full_name",
        "address", "streetaddress", "street_address",
        "passwordhash", "password_hash", "password"
    };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var updatedProperties = new List<LogEventProperty>();

        foreach (var property in logEvent.Properties)
        {
            // Redact known sensitive property names entirely
            if (SensitivePropertyNames.Contains(property.Key))
            {
                updatedProperties.Add(propertyFactory.CreateProperty(property.Key, Redacted));
                continue;
            }

            if (property.Value is ScalarValue scalarValue && scalarValue.Value is string stringValue)
            {
                var scrubbed = ScrubPhi(stringValue);
                if (!ReferenceEquals(scrubbed, stringValue))
                {
                    updatedProperties.Add(propertyFactory.CreateProperty(property.Key, scrubbed));
                }
            }
        }

        foreach (var prop in updatedProperties)
        {
            logEvent.AddOrUpdateProperty(prop);
        }
    }

    internal static string ScrubPhi(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = input;

        // SSN: 123-45-6789 or 123456789
        result = SsnPattern().Replace(result, Redacted);

        // Email addresses
        result = EmailPattern().Replace(result, Redacted);

        // US phone: (123) 456-7890, 123-456-7890, 123.456.7890, +1-123-456-7890
        result = PhonePattern().Replace(result, Redacted);

        // Date of birth patterns: MM/DD/YYYY, MM-DD-YYYY, YYYY-MM-DD
        result = DobPattern().Replace(result, Redacted);

        // Medical Record Number patterns: MRN-12345, MRN:12345
        result = MrnPattern().Replace(result, Redacted);

        // Insurance policy/group numbers: Policy-12345, Group-ABC123
        result = InsurancePattern().Replace(result, Redacted);

        // Credit card patterns: 1234-5678-9012-3456
        result = CreditCardPattern().Replace(result, Redacted);

        return result;
    }

    [GeneratedRegex(@"\b\d{3}-\d{2}-\d{4}\b|\b\d{9}\b", RegexOptions.Compiled)]
    private static partial Regex SsnPattern();

    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"(\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"\b(0[1-9]|1[0-2])[/\-](0[1-9]|[12]\d|3[01])[/\-](19|20)\d{2}\b|\b(19|20)\d{2}[/\-](0[1-9]|1[0-2])[/\-](0[1-9]|[12]\d|3[01])\b", RegexOptions.Compiled)]
    private static partial Regex DobPattern();

    [GeneratedRegex(@"\bMRN[-:]?\s?\d{4,10}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MrnPattern();

    [GeneratedRegex(@"\b(Policy|Group|Member)[-:#]?\s?[A-Z0-9]{4,15}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex InsurancePattern();

    [GeneratedRegex(@"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex CreditCardPattern();
}
