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
}