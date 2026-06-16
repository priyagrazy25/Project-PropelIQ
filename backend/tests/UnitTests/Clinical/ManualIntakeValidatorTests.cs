using Clinical.Application.Validators;
using Clinical.Application.DTOs;

namespace UnitTests.Clinical;

public sealed class ManualIntakeValidatorTests
{
    private readonly ManualIntakeValidator _validator = new();

    private static ManualIntakeRequest ValidRequest() => new(
        PatientId: Guid.NewGuid(),
        MedicalHistory: "No significant history",
        Symptoms: "Headache and fever",
        Allergies: "Penicillin",
        CurrentMedications: "Ibuprofen 200mg");

    [Fact]
    public async Task ValidRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidRequest());
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task EmptyPatientId_Fails()
    {
        var cmd = ValidRequest() with { PatientId = Guid.Empty };
        var result = await _validator.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Patient ID is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task MedicalHistory_EmptyOrNull_Passes(string? value)
    {
        var cmd = ValidRequest() with { MedicalHistory = value };
        var result = await _validator.ValidateAsync(cmd);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Symptoms_EmptyOrNull_Passes(string? value)
    {
        var cmd = ValidRequest() with { Symptoms = value };
        var result = await _validator.ValidateAsync(cmd);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Allergies_EmptyOrNull_Passes(string? value)
    {
        var cmd = ValidRequest() with { Allergies = value };
        var result = await _validator.ValidateAsync(cmd);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CurrentMedications_EmptyOrNull_Passes(string? value)
    {
        var cmd = ValidRequest() with { CurrentMedications = value };
        var result = await _validator.ValidateAsync(cmd);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task MedicalHistory_ExceedsMaxLength_Fails()
    {
        var cmd = ValidRequest() with { MedicalHistory = new string('a', 4001) };
        var result = await _validator.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Medical history must not exceed 4000 characters.");
    }

    [Fact]
    public async Task Symptoms_ExceedsMaxLength_Fails()
    {
        var cmd = ValidRequest() with { Symptoms = new string('a', 1001) };
        var result = await _validator.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Symptoms must not exceed 1000 characters.");
    }

    [Fact]
    public async Task Allergies_ExceedsMaxLength_Fails()
    {
        var cmd = ValidRequest() with { Allergies = new string('a', 1001) };
        var result = await _validator.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Allergies must not exceed 1000 characters.");
    }

    [Fact]
    public async Task CurrentMedications_ExceedsMaxLength_Fails()
    {
        var cmd = ValidRequest() with { CurrentMedications = new string('a', 2001) };
        var result = await _validator.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Current medications must not exceed 2000 characters.");
    }

    [Fact]
    public async Task AllFieldsEmpty_OnlyPatientIdFails()
    {
        var cmd = new ManualIntakeRequest(Guid.Empty, null, null, null, null);
        var result = await _validator.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Patient ID is required.");
    }
}
