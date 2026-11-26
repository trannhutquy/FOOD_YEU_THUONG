using FoodHub.Domain;
using Xunit;

namespace FoodHub.Domain.Tests;

public class DeliveryTests
{
    private static readonly DateTime Now = new(2026, 11, 26, 10, 0, 0, DateTimeKind.Utc);

    private static Order NewReadyOrder()
    {
        var restaurant = new Restaurant("Banh Mi Shop", Guid.NewGuid());
        var item = new MenuItem(restaurant.Id, "Pork banh mi", new Money(25000));
        var order = Order.Create(Guid.NewGuid(), restaurant, new Address("10 Hung Vuong", "P2", "Cao Lanh", "Dong Thap"));
        order.AddItem(item, 2);
        order.Accept();
        order.StartPreparing();
        order.MarkReady();
        return order;
    }

    [Fact]
    public void FullDeliveryLifecycle_WithValidSixDigitCode_CompletesAndReleasesShipper()
    {
        var order = NewReadyOrder();
        var shipper = new Shipper(Guid.NewGuid());

        var delivery = Delivery.Assign(order, shipper, Now);
        var plainCode = delivery.StartDelivery(order, Now.AddMinutes(5));

        Assert.Equal(OrderStatus.Delivering, order.Status);
        Assert.Equal(1, shipper.ActiveOrderCount);

        delivery.ConfirmDelivery(order, shipper, plainCode, Now.AddMinutes(20));

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(0, shipper.ActiveOrderCount);
        Assert.NotNull(delivery.DeliveredAtUtc);
    }

    [Fact]
    public void ConfirmDelivery_AfterLockAndRegenerate_SucceedsWithNewCode()
    {
        var order = NewReadyOrder();
        var shipper = new Shipper(Guid.NewGuid());
        var delivery = Delivery.Assign(order, shipper, Now);
        var plainCode = delivery.StartDelivery(order, Now.AddMinutes(5));
        var wrong = plainCode == "000000" ? "999999" : "000000";

        for (var i = 0; i < DeliveryCode.MaxFailedAttempts; i++)
        {
            Assert.Throws<DomainException>(() =>
                delivery.ConfirmDelivery(order, shipper, wrong, Now.AddMinutes(10 + i)));
        }

        Assert.Equal(OrderStatus.Delivering, order.Status);
        Assert.Equal(1, shipper.ActiveOrderCount);

        var regenerated = delivery.RegenerateCode(Now.AddMinutes(20));
        delivery.ConfirmDelivery(order, shipper, regenerated, Now.AddMinutes(21));

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(0, shipper.ActiveOrderCount);
    }

    [Fact]
    public void CancelDelivery_WhenAccepted_CancelsOrderAndReleasesShipper()
    {
        var restaurant = new Restaurant("Banh Mi Shop", Guid.NewGuid());
        var item = new MenuItem(restaurant.Id, "Pork banh mi", new Money(25000));
        var order = Order.Create(Guid.NewGuid(), restaurant);
        order.AddItem(item, 1);
        order.Accept();

        var shipper = new Shipper(Guid.NewGuid());
        var delivery = Delivery.Assign(order, shipper, Now);

        delivery.CancelDelivery(order, shipper, "Customer unavailable");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(0, shipper.ActiveOrderCount);
    }
}
