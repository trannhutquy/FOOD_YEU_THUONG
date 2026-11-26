using FoodHub.Application.Common;
using FoodHub.Application.Deliveries;
using FoodHub.Application.Orders;
using FoodHub.Domain;

namespace FoodHub.Api.Tests.Application;

public class DeliveryServiceTests
{
    private static readonly AddressDto DefaultAddress = new("10 Hung Vuong", "P2", "Cao Lanh", "Dong Thap");

    private static async Task<(InMemoryFoodHubHarness Harness, Guid OwnerId, Guid CustomerId, Guid ShipperUserId, Guid OrderId)>
        SetupReadyOrderAsync()
    {
        var harness = new InMemoryFoodHubHarness();
        var ownerId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipperUserId = Guid.NewGuid();
        var restaurant = new Restaurant("Pho 24", ownerId);
        var item = new MenuItem(restaurant.Id, "Pho bo", new Money(50000));
        var shipper = new Shipper(shipperUserId);

        harness.Store.AddRestaurant(restaurant);
        harness.Store.AddMenuItem(item);
        await harness.Store.AddAsync(shipper);

        var created = await harness.OrderService.CreateOrderAsync(
            customerId,
            new CreateOrderRequest(restaurant.Id, DefaultAddress, new[] { new OrderItemRequest(item.Id, 1) }));

        await harness.OrderService.AcceptOrderAsync(ownerId, created.Id);
        await harness.OrderService.StartPreparingAsync(ownerId, created.Id);
        await harness.OrderService.MarkReadyAsync(ownerId, created.Id);

        return (harness, ownerId, customerId, shipperUserId, created.Id);
    }

    [Fact]
    public async Task FullDeliveryWorkflow_PickUpAndConfirmWithCustomerCode_MarksDelivered()
    {
        var (harness, _, customerId, shipperUserId, orderId) = await SetupReadyOrderAsync();

        var pickupStatus = await harness.DeliveryService.PickUpOrderAsync(shipperUserId, orderId);
        Assert.Equal(OrderStatus.Delivering, pickupStatus.OrderStatus);

        var customerOrder = await harness.OrderService.GetOrderByIdAsync(customerId, UserRole.Customer, orderId);
        var code = customerOrder.DeliveryConfirmationCode!;

        var delivered = await harness.DeliveryService.ConfirmDeliveryAsync(
            shipperUserId,
            orderId,
            new ConfirmDeliveryRequest(code));

        Assert.Equal(OrderStatus.Delivered, delivered.OrderStatus);
        Assert.NotNull(delivered.DeliveredAtUtc);

        var shipper = await harness.Store.GetByUserIdAsync(shipperUserId);
        Assert.Equal(0, shipper!.ActiveOrderCount);
    }

    [Fact]
    public async Task ConfirmDelivery_WrongCodePersistsFailedAttempts_AndRestaurantCanRegenerateAfterLock()
    {
        var (harness, ownerId, customerId, shipperUserId, orderId) = await SetupReadyOrderAsync();
        await harness.DeliveryService.PickUpOrderAsync(shipperUserId, orderId);

        var customerOrder = await harness.OrderService.GetOrderByIdAsync(customerId, UserRole.Customer, orderId);
        var wrongCode = customerOrder.DeliveryConfirmationCode == "000000" ? "999999" : "000000";

        var savesBefore = harness.Store.SaveCount;
        for (var i = 0; i < DeliveryCode.MaxFailedAttempts; i++)
        {
            await Assert.ThrowsAsync<DomainException>(() =>
                harness.DeliveryService.ConfirmDeliveryAsync(shipperUserId, orderId, new ConfirmDeliveryRequest(wrongCode)));
        }

        Assert.Equal(savesBefore + DeliveryCode.MaxFailedAttempts, harness.Store.SaveCount);

        var regenerated = await harness.DeliveryService.RegenerateCodeAsync(ownerId, UserRole.RestaurantOwner, orderId);
        Assert.False(regenerated.IsCodeLocked);
        Assert.Equal(0, regenerated.CodeFailedAttempts);

        var refreshedCustomerOrder = await harness.OrderService.GetOrderByIdAsync(customerId, UserRole.Customer, orderId);
        var delivered = await harness.DeliveryService.ConfirmDeliveryAsync(
            shipperUserId,
            orderId,
            new ConfirmDeliveryRequest(refreshedCustomerOrder.DeliveryConfirmationCode!));

        Assert.Equal(OrderStatus.Delivered, delivered.OrderStatus);
    }

    [Fact]
    public async Task ConfirmDelivery_ByAnotherShipper_ThrowsForbidden()
    {
        var (harness, _, _, _, orderId) = await SetupReadyOrderAsync();
        var otherShipperUserId = Guid.NewGuid();
        await harness.Store.AddAsync(new Shipper(otherShipperUserId));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            harness.DeliveryService.ConfirmDeliveryAsync(otherShipperUserId, orderId, new ConfirmDeliveryRequest("123456")));
    }

    [Fact]
    public async Task UpdateAvailabilityAndGetAvailableOrders_WorkAsExpected()
    {
        var harness = new InMemoryFoodHubHarness();
        var ownerId = Guid.NewGuid();
        var shipperUserId = Guid.NewGuid();
        var restaurant = new Restaurant("Pho 24", ownerId);
        var item = new MenuItem(restaurant.Id, "Pho bo", new Money(50000));
        await harness.Store.AddAsync(new Shipper(shipperUserId));
        harness.Store.AddRestaurant(restaurant);
        harness.Store.AddMenuItem(item);

        var order = await harness.OrderService.CreateOrderAsync(
            Guid.NewGuid(),
            new CreateOrderRequest(restaurant.Id, DefaultAddress, new[] { new OrderItemRequest(item.Id, 1) }));
        await harness.OrderService.AcceptOrderAsync(ownerId, order.Id);

        var available = await harness.DeliveryService.GetAvailableOrdersAsync(shipperUserId);
        Assert.Single(available);
        Assert.Equal(order.Id, available[0].OrderId);

        var isAvailable = await harness.DeliveryService.UpdateAvailabilityAsync(
            shipperUserId,
            new UpdateAvailabilityRequest(false));
        Assert.False(isAvailable);
    }
}
