using Identity.Domain.Entities;

namespace UnitTests.Identity;

public class InsurancePlanEntityTests
{
    [Fact]
    public void InsurancePlan_DefaultValues_AreCorrect()
    {
        var plan = new InsurancePlan();

        Assert.NotEqual(Guid.Empty, plan.Id);
        Assert.Equal(string.Empty, plan.InsuranceName);
        Assert.Equal(string.Empty, plan.ValidMemberIdPattern);
        Assert.True(plan.IsActive);
        Assert.False(plan.IsDeleted);
        Assert.Null(plan.DeletedAt);
    }

    [Fact]
    public void InsurancePlan_SetProperties_Persists()
    {
        var plan = new InsurancePlan
        {
            InsuranceName = "Blue Cross Blue Shield",
            ValidMemberIdPattern = @"^[A-Z]{3}\d{9}$"
        };

        Assert.Equal("Blue Cross Blue Shield", plan.InsuranceName);
        Assert.Equal(@"^[A-Z]{3}\d{9}$", plan.ValidMemberIdPattern);
    }

    [Fact]
    public void InsurancePlan_MemberIdPattern_ValidatesFormat()
    {
        var plan = new InsurancePlan
        {
            InsuranceName = "Aetna",
            ValidMemberIdPattern = @"^\d{8,12}$"
        };

        var regex = new System.Text.RegularExpressions.Regex(plan.ValidMemberIdPattern);

        Assert.True(regex.IsMatch("12345678"));
        Assert.True(regex.IsMatch("123456789012"));
        Assert.False(regex.IsMatch("1234567"));
        Assert.False(regex.IsMatch("ABC12345678"));
    }

    [Fact]
    public void InsurancePlan_SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        var plan = new InsurancePlan
        {
            InsuranceName = "Test Insurance",
            ValidMemberIdPattern = @"^\d{10}$"
        };

        plan.IsDeleted = true;
        plan.DeletedAt = DateTime.UtcNow;

        Assert.True(plan.IsDeleted);
        Assert.NotNull(plan.DeletedAt);
    }

    [Fact]
    public void InsurancePlan_Deactivate_SetsIsActiveFalse()
    {
        var plan = new InsurancePlan
        {
            InsuranceName = "Expired Insurance",
            ValidMemberIdPattern = @"^\d{10}$"
        };

        plan.IsActive = false;

        Assert.False(plan.IsActive);
        Assert.False(plan.IsDeleted);
    }

    [Theory]
    [InlineData("Blue Cross Blue Shield", @"^[A-Z]{3}\d{9}$", "ABC123456789", true)]
    [InlineData("Blue Cross Blue Shield", @"^[A-Z]{3}\d{9}$", "AB1234567890", false)]
    [InlineData("UnitedHealthcare", @"^U\d{9}$", "U123456789", true)]
    [InlineData("UnitedHealthcare", @"^U\d{9}$", "X123456789", false)]
    [InlineData("Humana", @"^H\d{8}$", "H12345678", true)]
    [InlineData("Humana", @"^H\d{8}$", "H1234567890", false)]
    [InlineData("Cigna", @"^\d{10}$", "1234567890", true)]
    [InlineData("Cigna", @"^\d{10}$", "12345", false)]
    public void InsurancePlan_ValidMemberIdPattern_MatchesExpected(
        string name, string pattern, string memberId, bool expectedMatch)
    {
        var plan = new InsurancePlan
        {
            InsuranceName = name,
            ValidMemberIdPattern = pattern
        };

        var regex = new System.Text.RegularExpressions.Regex(plan.ValidMemberIdPattern);
        Assert.Equal(expectedMatch, regex.IsMatch(memberId));
    }
}
