using FoodHub.Domain;
using Xunit;

namespace FoodHub.Domain.Tests;

public class UserTests
{
    [Fact]
    public void Constructor_NormalizesEmailAndName()
    {
        var user = new User("  Vy@Mail.COM ", "hash", "  Vy ", UserRole.Customer);

        Assert.Equal("vy@mail.com", user.Email);
        Assert.Equal("Vy", user.FullName);
        Assert.Equal(UserRole.Customer, user.Role);
    }

    [Theory]
    [InlineData("", "hash", "Vy")]
    [InlineData("a@mail.com", "", "Vy")]
    [InlineData("a@mail.com", "hash", " ")]
    public void Constructor_MissingRequiredField_Throws(string email, string hash, string name)
    {
        Assert.Throws<DomainException>(() => new User(email, hash, name, UserRole.Customer));
    }
}

public class RefreshTokenTests
{
    [Fact]
    public void Revoke_SetsRevokedAndReplacement()
    {
        var token = new RefreshToken(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(7));
        Assert.False(token.IsRevoked);

        token.Revoke(DateTime.UtcNow, "new-hash");

        Assert.True(token.IsRevoked);
        Assert.Equal("new-hash", token.ReplacedByTokenHash);
    }

    [Fact]
    public void IsExpired_DependsOnTime()
    {
        var now = DateTime.UtcNow;
        var token = new RefreshToken(Guid.NewGuid(), "hash", now.AddMinutes(10));

        Assert.False(token.IsExpired(now));
        Assert.True(token.IsExpired(now.AddMinutes(10)));
    }

    [Fact]
    public void Constructor_EmptyUser_Throws()
    {
        Assert.Throws<DomainException>(() => new RefreshToken(Guid.Empty, "hash", DateTime.UtcNow.AddDays(1)));
    }
}
