using FoodHub.Domain;
using Xunit;

namespace FoodHub.Domain.Tests;

public class OrderStatusTests
{
    private static Order NewOrderWithItem()
    {
        var restaurant = new Restaurant("Rice House", Guid.NewGuid());
        var item = new MenuItem(restaurant.Id, "Grilled pork rice", new Money(45000));
        var order = Order.Create(Guid.NewGuid(), restaurant);
        order.AddItem(item, 1);
        return order;
    }

    [Fact]
    public void HappyPath_ReachesDelivered()
    {
        var order = NewOrderWithItem();
        var shipperId = Guid.NewGuid();

        order.Accept();
        order.AssignShipper(shipperId);
        order.StartPreparing();
        order.MarkReady();
        order.PickUp(shipperId);
        order.MarkDelivered();

        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void Reject_FromPending_SetsRejectedAndStoresReason()
    {
        var order = NewOrderWithItem();
        order.Reject("Out of ingredients");

        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.Equal("Out of ingredients", order.RejectionReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithoutReason_Throws(string reason)
    {
        var order = NewOrderWithItem();

        Assert.Throws<DomainException>(() => order.Reject(reason));
    }

    [Fact]
    public void Accept_Twice_Throws()
    {
        var order = NewOrderWithItem();
        order.Accept();

        Assert.Throws<DomainException>(() => order.Accept());
    }

    [Fact]
    public void MarkDelivered_FromPending_Throws()
    {
        var order = NewOrderWithItem();

        Assert.Throws<DomainException>(() => order.MarkDelivered());
    }

    [Fact]
    public void AssignShipper_WhenPending_Throws()
    {
        var order = NewOrderWithItem();

        Assert.Throws<DomainException>(() => order.AssignShipper(Guid.NewGuid()));
    }

    [Fact]
    public void AssignShipper_Twice_Throws()
    {
        var order = NewOrderWithItem();
        order.Accept();
        order.AssignShipper(Guid.NewGuid());

        Assert.Throws<DomainException>(() => order.AssignShipper(Guid.NewGuid()));
    }

    [Fact]
    public void PickUp_ByDifferentShipper_Throws()
    {
        var order = NewOrderWithItem();
        order.Accept();
        order.AssignShipper(Guid.NewGuid());
        order.StartPreparing();
        order.MarkReady();

        Assert.Throws<DomainException>(() => order.PickUp(Guid.NewGuid()));
    }
}