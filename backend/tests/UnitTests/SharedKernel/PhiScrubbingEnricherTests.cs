using SharedKernel.Logging;

namespace UnitTests.SharedKernel;

public class PhiScrubbingEnricherTests
{
    private const string Redacted = "[PHI-REDACTED]";

    [Theory]
    [InlineData("SSN is 123-45-6789", "SSN is [PHI-REDACTED]")]
    [InlineData("Patient SSN: 987-65-4321 on file", "Patient SSN: [PHI-REDACTED] on file")]
    public void ScrubPhi_RedactsSSN(string input, string expected)
    {
        var result = PhiScrubbingEnricher.ScrubPhi(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Email: patient@hospital.com", "Email: [PHI-REDACTED]")]
    [InlineData("Contact john.doe@example.org for info", "Contact [PHI-REDACTED] for info")]
    public void ScrubPhi_RedactsEmail(string input, string expected)
    {
        var result = PhiScrubbingEnricher.ScrubPhi(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Phone: (555) 123-4567")]
    [InlineData("Call 555-123-4567")]
    [InlineData("Reach at 555.123.4567")]
    [InlineData("International +1-555-123-4567")]
    public void ScrubPhi_RedactsPhoneNumbers(string input)
    {
        var result = PhiScrubbingEnricher.ScrubPhi(input);
        Assert.Contains(Redacted, result);
        Assert.DoesNotContain("555", result);
    }

    [Theory]
    [InlineData("DOB: 01/15/1990")]
    [InlineData("Born 1990-01-15")]
    [InlineData("Date: 12-31-2000")]
    public void ScrubPhi_RedactsDatesOfBirth(string input)
    {
        var result = PhiScrubbingEnricher.ScrubPhi(input);
        Assert.Contains(Redacted, result);
    }

    [Theory]
    [InlineData("MRN-12345", "[PHI-REDACTED]")]
    [InlineData("MRN:67890", "[PHI-REDACTED]")]
    [InlineData("Record MRN 54321 found", "Record [PHI-REDACTED] found")]
    public void ScrubPhi_RedactsMRN(string input, string expected)
    {
        var result = PhiScrubbingEnricher.ScrubPhi(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Normal log message without PHI")]
    [InlineData("Request completed in 42ms")]
    public void ScrubPhi_LeavesNonPhiUnchanged(string input)
    {
        var result = PhiScrubbingEnricher.ScrubPhi(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void ScrubPhi_HandlesNullInput()
    {
        var result = PhiScrubbingEnricher.ScrubPhi(null!);
        Assert.Null(result);
    }

    [Fact]
    public void ScrubPhi_RedactsMultiplePatterns()
    {
        var input = "Patient email: test@mail.com, SSN: 123-45-6789, Phone: 555-123-4567";
        var result = PhiScrubbingEnricher.ScrubPhi(input);

        Assert.DoesNotContain("test@mail.com", result);
        Assert.DoesNotContain("123-45-6789", result);
        Assert.DoesNotContain("555-123-4567", result);
        Assert.Equal(3, CountOccurrences(result, Redacted));
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
