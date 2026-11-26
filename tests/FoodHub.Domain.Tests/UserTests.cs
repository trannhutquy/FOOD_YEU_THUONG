using FoodHub.Domain;
using Xunit;

namespace FoodHub.Domain.Tests;

public class UserTests
{
    [Fact]
    public void CreateUser_WithValidParameters_Succeeds()
    {
        var id = Guid.NewGuid();
        var user = new User(
            "Thang@Example.com",
            "hashed_pwd_123",
            "Le Ha Huu Thang",
            UserRole.Customer,
            "0901234567",
            id);

        Assert.Equal(id, user.Id);
        Assert.Equal("thang@example.com", user.Email); // lowercase normalized
        Assert.Equal("hashed_pwd_123", user.PasswordHash);
        Assert.Equal("Le Ha Huu Thang", user.FullName);
        Assert.Equal(UserRole.Customer, user.Role);
        Assert.Equal("0901234567", user.PhoneNumber);
    }

    [Theory]
    [InlineData("", "pwd", "Name", "Role")]
    [InlineData("email@a.com", "", "Name", "Role")]
    [InlineData("email@a.com", "pwd", "", "Role")]
    [InlineData("email@a.com", "pwd", "Name", "")]
    public void CreateUser_WithEmptyRequiredFields_ThrowsDomainException(
        string email, string pwd, string name, string role)
    {
        Assert.Throws<DomainException>(() => new User(email, pwd, name, role));
    }
}
