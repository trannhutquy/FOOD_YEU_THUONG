using FoodHub.Domain;
using Xunit;

namespace FoodHub.Domain.Tests;

public class ShipperTests
{
    private static Order NewAcceptedOrder()
    {
        var restaurant = new Restaurant("Pho House", Guid.NewGuid());
        var item = new MenuItem(restaurant.Id, "Beef pho", new Money(50000));
        var order = Order.Create(Guid.NewGuid(), restaurant);
        order.AddItem(item, 1);
        order.Accept();
        return order;
    }

    [Fact]
    public void AcceptOrder_ThirdActiveOrder_Throws()
    {
        var shipper = new Shipper(Guid.NewGuid());

        shipper.AcceptOrder(NewAcceptedOrder());
        shipper.AcceptOrder(NewAcceptedOrder());

        Assert.Equal(2, shipper.ActiveOrderCount);
        Assert.False(shipper.CanAcceptOrder());
        Assert.Throws<DomainException>(() => shipper.AcceptOrder(NewAcceptedOrder()));
    }

    [Fact]
    public void AcceptOrder_WhenUnavailable_Throws()
    {
        var shipper = new Shipper(Guid.NewGuid(), isAvailable: false);

        Assert.Throws<DomainException>(() => shipper.AcceptOrder(NewAcceptedOrder()));
    }

    [Fact]
    public void ReleaseOrder_WhenDeliveredOrCancelled_FreesSlotForNewOrder()
    {
        var shipper = new Shipper(Guid.NewGuid());
        var order1 = NewAcceptedOrder();
        var order2 = NewAcceptedOrder();

        shipper.AcceptOrder(order1);
        shipper.AcceptOrder(order2);

        order1.Cancel("Customer changed mind");
        shipper.ReleaseOrder(order1);

        Assert.Equal(1, shipper.ActiveOrderCount);
        Assert.True(shipper.CanAcceptOrder());

        var order3 = NewAcceptedOrder();
        shipper.AcceptOrder(order3);
        Assert.Equal(2, shipper.ActiveOrderCount);
    }

    [Fact]
    public void ReleaseOrder_WhileDelivering_Throws()
    {
        var shipper = new Shipper(Guid.NewGuid());
        var order = NewAcceptedOrder();

        shipper.AcceptOrder(order);
        order.StartPreparing();
        order.MarkReady();
        order.PickUp(shipper.Id);

        Assert.Throws<DomainException>(() => shipper.ReleaseOrder(order));
    }
}
