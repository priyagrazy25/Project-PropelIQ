using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace UnitTests.Identity;

public class UserEntityTests
{
    [Fact]
    public void NewUser_ShouldHaveGuidId()
    {
        var user = new User();
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void NewUser_ShouldDefaultToPendingVerification()
    {
        var user = new User();
        Assert.Equal(UserStatus.PendingVerification, user.Status);
    }

    [Fact]
    public void NewUser_ShouldDefaultToPatientRole()
    {
        var user = new User();
        Assert.Equal(UserRole.Patient, user.Role);
    }

    [Fact]
    public void NewUser_ShouldNotBeSoftDeleted()
    {
        var user = new User();
        Assert.False(user.IsDeleted);
    }

    [Fact]
    public void NewUser_ShouldHaveTimestamps()
    {
        var before = DateTime.UtcNow;
        var user = new User();
        Assert.True(user.CreatedAt >= before);
        Assert.True(user.UpdatedAt >= before);
    }
}
