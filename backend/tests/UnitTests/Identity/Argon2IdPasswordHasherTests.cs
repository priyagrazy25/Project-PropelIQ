using Identity.Infrastructure.Services;

namespace UnitTests.Identity;

public sealed class Argon2IdPasswordHasherTests
{
    private readonly Argon2IdPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesSaltAndHash()
    {
        var hash = _hasher.Hash("TestP@ssword1");

        Assert.NotNull(hash);
        Assert.Contains(":", hash);
        var parts = hash.Split(':');
        Assert.Equal(2, parts.Length);
        Assert.NotEmpty(parts[0]); // salt
        Assert.NotEmpty(parts[1]); // hash
    }

    [Fact]
    public void Hash_DifferentCallsProduceDifferentResults()
    {
        var hash1 = _hasher.Hash("TestP@ssword1");
        var hash2 = _hasher.Hash("TestP@ssword1");

        Assert.NotEqual(hash1, hash2); // Different salts
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("MyP@ssword1");

        Assert.True(_hasher.Verify("MyP@ssword1", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("MyP@ssword1");

        Assert.False(_hasher.Verify("WrongPassword", hash));
    }

    [Fact]
    public void Verify_MalformedHash_ReturnsFalse()
    {
        Assert.False(_hasher.Verify("password", "not-a-valid-hash"));
    }
}
