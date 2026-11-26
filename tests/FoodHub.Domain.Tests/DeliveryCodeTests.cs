using FoodHub.Domain;
using Xunit;

namespace FoodHub.Domain.Tests;

public class DeliveryCodeTests
{
    private static readonly DateTime Now = new(2026, 11, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_GeneratesSixDigitHashedCode()
    {
        var (code, plain) = DeliveryCode.Create(Now);

        Assert.Equal(6, plain.Length);
        Assert.True(plain.All(char.IsAsciiDigit));
        Assert.NotEqual(plain, code.CodeHash);
        Assert.False(code.IsUsed);
        Assert.False(code.IsLocked);
    }

    [Fact]
    public void VerifyOrThrow_ValidCode_MarksUsedAndCannotReuse()
    {
        var (code, plain) = DeliveryCode.Create(Now);

        code.VerifyOrThrow(plain, Now.AddMinutes(10));

        Assert.True(code.IsUsed);
        Assert.Throws<DomainException>(() => code.VerifyOrThrow(plain, Now.AddMinutes(11)));
    }

    [Fact]
    public void VerifyOrThrow_ExpiredCode_Throws()
    {
        var (code, plain) = DeliveryCode.Create(Now, TimeSpan.FromHours(2));

        Assert.Throws<DomainException>(() => code.VerifyOrThrow(plain, Now.AddHours(2).AddSeconds(1)));
    }

    [Fact]
    public void VerifyOrThrow_FiveWrongAttempts_LocksCodeEvenForValidAttemptAfterward()
    {
        var (code, plain) = DeliveryCode.Create(Now);
        var wrong = plain == "000000" ? "999999" : "000000";

        for (var i = 0; i < DeliveryCode.MaxFailedAttempts; i++)
        {
            Assert.Throws<DomainException>(() => code.VerifyOrThrow(wrong, Now.AddMinutes(i + 1)));
        }

        Assert.True(code.IsLocked);
        Assert.Equal(DeliveryCode.MaxFailedAttempts, code.FailedAttempts);
        Assert.Throws<DomainException>(() => code.VerifyOrThrow(plain, Now.AddMinutes(10)));
    }

    [Fact]
    public void Reset_AfterLock_GeneratesNewWorkingCode()
    {
        var (code, plain) = DeliveryCode.Create(Now);
        var wrong = plain == "000000" ? "999999" : "000000";

        for (var i = 0; i < DeliveryCode.MaxFailedAttempts; i++)
        {
            Assert.Throws<DomainException>(() => code.VerifyOrThrow(wrong, Now.AddMinutes(1)));
        }

        var newPlain = code.Reset(Now.AddMinutes(5));

        Assert.False(code.IsLocked);
        Assert.Equal(0, code.FailedAttempts);
        code.VerifyOrThrow(newPlain, Now.AddMinutes(6));
        Assert.True(code.IsUsed);
    }
}
