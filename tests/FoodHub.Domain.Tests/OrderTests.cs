using FoodHub.Domain;
using Xunit;

namespace FoodHub.Domain.Tests;

public class OrderTests
{

    private static (Restaurant, MenuItem) NewMenuItem(bool available = true)
    {
        var restaurant = new Restaurant("Rice House", Guid.NewGuid());
        var item = new MenuItem(restaurant.Id, "Grilled pork rice", new Money(45000), available);
        return (restaurant, item);
    }

    [Fact]
    public void AddItem_FromAnotherRestaurant_Throws()
    {
        var (restaurant, _) = NewMenuItem();
        var otherItem = new MenuItem(Guid.NewGuid(), "Noodles", new Money(30000));
        var order = Order.Create(Guid.NewGuid(), restaurant);

        Assert.Throws<DomainException>(() => order.AddItem(otherItem, 1));
    }

    [Fact]
    public void AddItem_UnavailableItem_Throws()
    {
        var (restaurant, item) = NewMenuItem(available: false);
        var order = Order.Create(Guid.NewGuid(), restaurant);

        Assert.Throws<DomainException>(() => order.AddItem(item, 1));
    }

    [Fact]
    public void Cancel_AfterPreparing_Throws()
    {
        var (restaurant, item) = NewMenuItem();
        var order = Order.Create(Guid.NewGuid(), restaurant);
        order.AddItem(item, 1);
        order.Accept();
        order.StartPreparing();

        Assert.Throws<DomainException>(() => order.Cancel());
    }

    [Fact]
    public void Accept_EmptyOrder_Throws()
    {
        var (restaurant, _) = NewMenuItem();
        var order = Order.Create(Guid.NewGuid(), restaurant);

        Assert.Throws<DomainException>(() => order.Accept());
    }

    [Fact]
    public void UpdateAndRemoveItem_WhilePending_UpdatesTotals()
    {
        var (restaurant, item) = NewMenuItem();
        var secondItem = new MenuItem(restaurant.Id, "Iced tea", new Money(10000));
        var order = Order.Create(Guid.NewGuid(), restaurant, shippingFee: new Money(15000));

        order.AddItem(item, 1);
        order.AddItem(secondItem, 2);
        order.UpdateItemQuantity(secondItem.Id, 3);
        Assert.Equal(90000m, order.Total.Amount);

        order.RemoveItem(secondItem.Id);
        Assert.Single(order.Items);
        Assert.Equal(60000m, order.Total.Amount);
    }

    [Fact]
    public void ApplyDiscount_CapsAtOrderSubtotalPlusShipping()
    {
        var (restaurant, item) = NewMenuItem();
        var order = Order.Create(Guid.NewGuid(), restaurant, shippingFee: new Money(15000));
        order.AddItem(item, 1);

        order.ApplyDiscount("VIP200", new Money(120000));

        Assert.Equal(60000m, order.DiscountAmount.Amount);
        Assert.Equal(0m, order.Total.Amount);
        Assert.Equal("VIP200", order.AppliedVoucherCode);
    }

    [Fact]
    public void ApplyDiscount_FreeShipping_ReducesTotalByShippingFee()
    {
        var (restaurant, item) = NewMenuItem();
        var order = Order.Create(Guid.NewGuid(), restaurant, shippingFee: new Money(15000));
        order.AddItem(item, 1);

        order.ApplyDiscount("FREESHIP", new Money(15000));

        Assert.Equal(15000m, order.DiscountAmount.Amount);
        Assert.Equal(45000m, order.Total.Amount);
    }

    [Fact]
    public void Address_ValueEqualityAndValidation_Work()
    {
        var a1 = new Address("12 Le Loi", "Phuong 1", "Cao Lanh", "Dong Thap", "Call on arrival");
        var a2 = new Address("12 Le Loi", "Phuong 1", "Cao Lanh", "Dong Thap", "Call on arrival");

        Assert.Equal(a1, a2);
        Assert.Throws<DomainException>(() => new Address("", "Phuong 1", "Cao Lanh", "Dong Thap"));
    }

    [Fact]
    public void Money_SubtractExceedingBalance_Throws()
    {
        Assert.Throws<DomainException>(() => new Money(10000) - new Money(20000));
    }
}
