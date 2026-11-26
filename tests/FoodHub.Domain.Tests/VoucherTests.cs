using FoodHub.Domain;
using Xunit;

namespace FoodHub.Domain.Tests;

public class VoucherTests
{
    private static readonly DateTime Now = new(2026, 11, 20, 8, 0, 0, DateTimeKind.Utc);

    private static Voucher Percent10Capped() =>
        new("SALE10", VoucherType.Percentage, 10, 20000, 50000, Now.AddDays(7), 100);

    [Fact]
    public void Percentage_IsCappedAtMaxDiscount()
    {
        var discount = Percent10Capped()
            .Redeem(Guid.NewGuid(), new Money(500000), new Money(15000), Now);

        Assert.Equal(20000m, discount.Amount);
    }

    [Fact]
    public void Percentage_WithoutCap_CalculatesRoundedPercent()
    {
        var voucher = new Voucher("SALE15", VoucherType.Percentage, 15, null, 0, Now.AddDays(7), 10);

        var discount = voucher.Preview(Guid.NewGuid(), new Money(100000), new Money(15000), Now);

        Assert.Equal(15000m, discount.Amount);
    }

    [Fact]
    public void FixedAmount_CannotExceedSubtotal()
    {
        var voucher = new Voucher("FLAT50K", VoucherType.FixedAmount, 50000, null, 0, Now.AddDays(7), 10);

        var discount = voucher.Redeem(Guid.NewGuid(), new Money(30000), new Money(15000), Now);

        Assert.Equal(30000m, discount.Amount);
    }

    [Fact]
    public void FreeShipping_ReturnsShippingFee()
    {
        var voucher = new Voucher("FREESHIP", VoucherType.FreeShipping, 0, null, 50000, Now.AddDays(7), 10);

        var discount = voucher.Redeem(Guid.NewGuid(), new Money(60000), new Money(18000), Now);

        Assert.Equal(18000m, discount.Amount);
    }

    [Fact]
    public void Redeem_BelowMinimum_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Percent10Capped().Redeem(Guid.NewGuid(), new Money(40000), new Money(15000), Now));
    }

    [Fact]
    public void Redeem_ExpiredVoucher_Throws()
    {
        var voucher = Percent10Capped();

        Assert.Throws<DomainException>(() =>
            voucher.Redeem(Guid.NewGuid(), new Money(100000), new Money(15000), Now.AddDays(8)));
    }

    [Fact]
    public void Redeem_UsageLimitReached_Throws()
    {
        var voucher = new Voucher("ONCE", VoucherType.FixedAmount, 10000, null, 0, Now.AddDays(7), 1);
        voucher.Redeem(Guid.NewGuid(), new Money(100000), new Money(15000), Now);

        Assert.Throws<DomainException>(() =>
            voucher.Redeem(Guid.NewGuid(), new Money(100000), new Money(15000), Now));
    }

    [Fact]
    public void Redeem_SameCustomerTwice_Throws()
    {
        var voucher = Percent10Capped();
        var customer = Guid.NewGuid();
        voucher.Redeem(customer, new Money(100000), new Money(15000), Now);

        Assert.Throws<DomainException>(() =>
            voucher.Redeem(customer, new Money(100000), new Money(15000), Now));
    }

    [Fact]
    public void Preview_DoesNotIncrementUsedCountOrVersion()
    {
        var voucher = Percent10Capped();
        var customer = Guid.NewGuid();

        var discount = voucher.Preview(customer, new Money(100000), new Money(15000), Now);

        Assert.Equal(10000m, discount.Amount);
        Assert.Equal(0, voucher.UsedCount);
        Assert.Equal(0, voucher.Version);
        Assert.Empty(voucher.RedeemedBy);
    }

    [Fact]
    public void Refund_RestoresUsageCountAndAllowsReRedeem()
    {
        var voucher = new Voucher("REFUND1", VoucherType.FixedAmount, 10000, null, 0, Now.AddDays(7), 1);
        var customer = Guid.NewGuid();
        voucher.Redeem(customer, new Money(100000), new Money(15000), Now);
        Assert.Equal(1, voucher.UsedCount);

        voucher.Refund(customer);

        Assert.Equal(0, voucher.UsedCount);
        Assert.Empty(voucher.RedeemedBy);
        var discount = voucher.Redeem(customer, new Money(100000), new Money(15000), Now);
        Assert.Equal(10000m, discount.Amount);
    }

    [Theory]
    [InlineData("", VoucherType.FixedAmount, 10000, 10)]
    [InlineData("V1", VoucherType.FixedAmount, 0, 10)]
    [InlineData("V2", VoucherType.Percentage, 101, 10)]
    [InlineData("V3", VoucherType.FixedAmount, 10000, 0)]
    public void Constructor_InvalidParameters_Throws(string code, VoucherType type, decimal value, int usageLimit)
    {
        Assert.Throws<DomainException>(() =>
            new Voucher(code, type, value, null, 0, Now.AddDays(1), usageLimit));
    }
}

public class RestaurantAndMenuItemTests
{
    [Fact]
    public void Restaurant_RenameAndSetOpen_WorkAndValidate()
    {
        var restaurant = new Restaurant("  Bun Bo Hue  ", Guid.NewGuid());
        Assert.Equal("Bun Bo Hue", restaurant.Name);
        Assert.True(restaurant.IsOpen);

        restaurant.Rename("  Bun Bo Co Ba  ");
        restaurant.SetOpen(false);

        Assert.Equal("Bun Bo Co Ba", restaurant.Name);
        Assert.False(restaurant.IsOpen);
        Assert.Throws<DomainException>(() => restaurant.Rename(" "));
    }

    [Fact]
    public void MenuItem_RenameChangePriceAndAvailability_WorkAndValidate()
    {
        var item = new MenuItem(Guid.NewGuid(), "  Pho Bo  ", new Money(45000));
        Assert.Equal("Pho Bo", item.Name);

        item.Rename("Pho Dac Biet");
        item.ChangePrice(new Money(55000));
        item.SetAvailability(false);

        Assert.Equal("Pho Dac Biet", item.Name);
        Assert.Equal(55000m, item.Price.Amount);
        Assert.False(item.IsAvailable);
        Assert.Throws<DomainException>(() => item.ChangePrice(Money.Zero));
        Assert.Throws<DomainException>(() => item.Rename(""));
    }
}
