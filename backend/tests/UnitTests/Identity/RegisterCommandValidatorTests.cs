using Identity.Application.Commands.Register;

namespace UnitTests.Identity;

public sealed class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private static RegisterCommand ValidCommand() => new(
        FirstName: "John",
        LastName: "Doe",
        Email: "john.doe@example.com",
        Phone: "(555) 123-4567",
        DateOfBirth: "1990-01-15",
        Gender: "Male",
        Password: "P@ssword1");

    [Fact]
    public async Task ValidCommand_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidCommand());
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "First name is required.")]
    [InlineData(null, "First name is required.")]
    public async Task FirstName_Empty_Fails(string? firstName, string expectedError)
    {
        var cmd = ValidCommand() with { FirstName = firstName! };
        var result = await _validator.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData("", "Last name is required.")]
    [InlineData(null, "Last name is required.")]
    public async Task LastName_Empty_Fails(string? lastName, string expectedError)
    {
        var cmd = ValidCommand() with { LastName = lastName! };
        var result = await _validator.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData("", "Email address is required.")]
    [InlineData("notanemail", "Please enter a valid email address.")]
    public async Task Email_Invalid_Fails(string email, string expectedError)
    {
        var cmd = ValidCommand() with { Email = email };
        var result = await _validator.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public async Task Phone_Invalid_Fails(string phone)
    {
        var cmd = ValidCommand() with { Phone = phone };
        var result = await _validator.ValidateAsync(cmd);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-date")]
    public async Task DateOfBirth_Invalid_Fails(string dob)
    {
        var cmd = ValidCommand() with { DateOfBirth = dob };
        var result = await _validator.ValidateAsync(cmd);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task DateOfBirth_InFuture_Fails()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)).ToString("yyyy-MM-dd");
        var cmd = ValidCommand() with { DateOfBirth = futureDate };
        var result = await _validator.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Date of birth cannot be in the future.");
    }

    [Theory]
    [InlineData("", "Password is required.")]
    [InlineData("short1!", "Password must be at least 8 characters.")]
    [InlineData("alllowercase1!", "Password must contain at least one uppercase letter.")]
    [InlineData("NoNumbers!!", "Password must contain at least one number.")]
    [InlineData("NoSpecial1a", "Password must contain at least one special character.")]
    public async Task Password_Invalid_Fails(string password, string expectedError)
    {
        var cmd = ValidCommand() with { Password = password };
        var result = await _validator.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == expectedError);
    }

    [Fact]
    public async Task Email_TooLong_Fails()
    {
        var longEmail = new string('a', 251) + "@x.com"; // 257 chars
        var cmd = ValidCommand() with { Email = longEmail };
        var result = await _validator.ValidateAsync(cmd);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Email must not exceed 256 characters.");
    }
}
