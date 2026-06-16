using Clinical.Infrastructure.AI;

namespace UnitTests.Clinical;

/// <summary>
/// Unit tests for PiiRedactor verifying HIPAA identifier redaction (AIR-S02).
/// </summary>
public sealed class PiiRedactorTests
{
    private readonly PiiRedactor _redactor = new();

    [Fact]
    public void Redact_EmptyString_ReturnsEmpty()
    {
        var (result, count) = _redactor.Redact(string.Empty);

        Assert.Equal(string.Empty, result);
        Assert.Equal(0, count);
    }

    [Fact]
    public void Redact_NullString_ReturnsNull()
    {
        var (result, count) = _redactor.Redact(null!);

        Assert.Null(result);
        Assert.Equal(0, count);
    }

    [Fact]
    public void Redact_TextWithoutPii_ReturnsUnchanged()
    {
        const string input = "Patient presents with mild headache and fatigue.";

        var (result, count) = _redactor.Redact(input);

        Assert.Equal(input, result);
        Assert.Equal(0, count);
    }

    [Theory]
    [InlineData("SSN: 123-45-6789", "SSN: [REDACTED]")]
    [InlineData("SSN: 123456789", "SSN: [REDACTED]")]
    [InlineData("SSN: 123 45 6789", "SSN: [REDACTED]")]
    public void Redact_SSN_IsRedacted(string input, string expected)
    {
        var (result, count) = _redactor.Redact(input);

        Assert.Equal(expected, result);
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData("Phone: (555) 123-4567", "Phone: [REDACTED]")]
    [InlineData("Phone: 555-123-4567", "Phone: [REDACTED]")]
    [InlineData("Phone: 555.123.4567", "Phone: [REDACTED]")]
    [InlineData("Phone: +1-555-123-4567", "Phone: [REDACTED]")]
    public void Redact_PhoneNumber_IsRedacted(string input, string expected)
    {
        var (result, count) = _redactor.Redact(input);

        Assert.Equal(expected, result);
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData("Email: patient@example.com", "Email: [REDACTED]")]
    [InlineData("Contact: john.doe@hospital.org", "Contact: [REDACTED]")]
    public void Redact_Email_IsRedacted(string input, string expected)
    {
        var (result, count) = _redactor.Redact(input);

        Assert.Equal(expected, result);
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData("DOB: 01/15/1985", "DOB: [REDACTED]")]
    [InlineData("DOB: 1985-01-15", "DOB: [REDACTED]")]
    [InlineData("Born: January 15, 1985", "Born: [REDACTED]")]
    [InlineData("Born: Jan 15, 1985", "Born: [REDACTED]")]
    public void Redact_DateOfBirth_IsRedacted(string input, string expected)
    {
        var (result, count) = _redactor.Redact(input);

        Assert.Equal(expected, result);
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData("MRN: 1234567", "MRN: [REDACTED]")]
    [InlineData("MR#12345678", "MR#[REDACTED]")]
    [InlineData("Medical Record Number: 123456789", "Medical Record Number: [REDACTED]")]
    public void Redact_MedicalRecordNumber_IsRedacted(string input, string expected)
    {
        var (result, count) = _redactor.Redact(input);

        Assert.Equal(expected, result);
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData("Address: 123 Main Street", "Address: [REDACTED]")]
    [InlineData("Lives at 456 Oak Avenue", "Lives at [REDACTED]")]
    [InlineData("Home: 789 Pine Boulevard", "Home: [REDACTED]")]
    public void Redact_StreetAddress_IsRedacted(string input, string expected)
    {
        var (result, count) = _redactor.Redact(input);

        Assert.Equal(expected, result);
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData("ZIP: 12345", "ZIP: [REDACTED]")]
    [InlineData("ZIP: 12345-6789", "ZIP: [REDACTED]")]
    public void Redact_ZipCode_IsRedacted(string input, string expected)
    {
        var (result, count) = _redactor.Redact(input);

        Assert.Equal(expected, result);
        Assert.Equal(1, count);
    }

    [Fact]
    public void Redact_MultiplePiiTypes_AllRedacted()
    {
        const string input = "Patient John Doe, SSN: 123-45-6789, Phone: (555) 123-4567, " +
                             "Email: john.doe@example.com, DOB: 01/15/1985";

        var (result, count) = _redactor.Redact(input);

        Assert.DoesNotContain("123-45-6789", result);
        Assert.DoesNotContain("(555) 123-4567", result);
        Assert.DoesNotContain("john.doe@example.com", result);
        Assert.DoesNotContain("01/15/1985", result);
        Assert.Equal(4, count);
    }

    [Fact]
    public void Redact_ClinicalTextWithPii_PreservesClinicalContent()
    {
        const string input = "Patient presents with hypertension. " +
                             "Contact: patient@email.com. " +
                             "Blood pressure: 140/90 mmHg. " +
                             "Prescribed Lisinopril 10mg daily.";

        var (result, count) = _redactor.Redact(input);

        Assert.Contains("hypertension", result);
        Assert.Contains("Blood pressure: 140/90 mmHg", result);
        Assert.Contains("Lisinopril 10mg daily", result);
        Assert.DoesNotContain("patient@email.com", result);
        Assert.Equal(1, count);
    }
}
