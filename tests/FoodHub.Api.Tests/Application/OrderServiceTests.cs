using FoodHub.Application.Abstractions;
using FoodHub.Application.Common;
using FoodHub.Application.Orders;
using FoodHub.Domain;

namespace FoodHub.Api.Tests.Application;

public class OrderServiceTests
{
    private sealed class FixedVoucherEvaluator(decimal amount) : IVoucherEvaluator
    {
        private sealed class FixedDiscount(decimal value) : IDiscountStrategy
        {
            public Money CalculateDiscount(Money subtotal, Money shippingFee) => new(value);
        }

        public Task<IDiscountStrategy> ResolveDiscountAsync(
            Guid customerId,
            string voucherCode,
            Money subtotal,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IDiscountStrategy>(new FixedDiscount(amount));
    }

    private static readonly AddressDto DefaultAddress = new("12 Le Loi", "P1", "Cao Lanh", "Dong Thap");

    [Fact]
    public async Task CreateOrder_WithValidItemsAndVoucher_ComputesTotalsAndSaves()
    {
        var harness = new InMemoryFoodHubHarness(new FixedVoucherEvaluator(10000));
        var ownerId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var restaurant = new Restaurant("Com Tam", ownerId);
        var item = new MenuItem(restaurant.Id, "Suon bi", new Money(45000));

        harness.Store.AddRestaurant(restaurant);
        harness.Store.AddMenuItem(item);

        var result = await harness.OrderService.CreateOrderAsync(
            customerId,
            new CreateOrderRequest(
                restaurant.Id,
                DefaultAddress,
                new[] { new OrderItemRequest(item.Id, 2) },
                VoucherCode: "GIAM10K",
                ShippingFee: 15000));

        Assert.Equal(OrderStatus.Pending, result.Status);
        Assert.Equal(90000m, result.Subtotal);
        Assert.Equal(15000m, result.ShippingFee);
        Assert.Equal(10000m, result.DiscountAmount);
        Assert.Equal(95000m, result.Total);
        Assert.Equal("GIAM10K", result.AppliedVoucherCode);
        Assert.Equal(1, harness.Store.SaveCount);
    }

    [Fact]
    public async Task CreateOrder_ItemsFromAnotherRestaurant_ThrowsDomainException()
    {
        var harness = new InMemoryFoodHubHarness();
        var restaurant = new Restaurant("Com Tam", Guid.NewGuid());
        var otherItem = new MenuItem(Guid.NewGuid(), "Pho", new Money(50000));

        harness.Store.AddRestaurant(restaurant);
        harness.Store.AddMenuItem(otherItem);

        await Assert.ThrowsAsync<DomainException>(() =>
            harness.OrderService.CreateOrderAsync(
                Guid.NewGuid(),
                new CreateOrderRequest(
                    restaurant.Id,
                    DefaultAddress,
                    new[] { new OrderItemRequest(otherItem.Id, 1) })));
    }

    [Fact]
    public async Task CancelOrder_AfterShipperAccepted_ReleasesShipperSlot()
    {
        var harness = new InMemoryFoodHubHarness();
        var ownerId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipperUserId = Guid.NewGuid();
        var restaurant = new Restaurant("Com Tam", ownerId);
        var item = new MenuItem(restaurant.Id, "Suon bi", new Money(45000));
        var shipper = new Shipper(shipperUserId);

        harness.Store.AddRestaurant(restaurant);
        harness.Store.AddMenuItem(item);
        await harness.Store.AddAsync(shipper);

        var created = await harness.OrderService.CreateOrderAsync(
            customerId,
            new CreateOrderRequest(restaurant.Id, DefaultAddress, new[] { new OrderItemRequest(item.Id, 1) }));

        await harness.OrderService.AcceptOrderAsync(ownerId, created.Id);
        await harness.DeliveryService.AcceptDeliveryAsync(shipperUserId, created.Id);
        Assert.Equal(1, shipper.ActiveOrderCount);

        var cancelled = await harness.OrderService.CancelOrderAsync(
            customerId,
            created.Id,
            new CancelOrderRequest("Changed my mind"));

        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
        Assert.Equal(0, shipper.ActiveOrderCount);
    }

    [Fact]
    public async Task AcceptOrder_ByDifferentRestaurantOwner_ThrowsForbidden()
    {
        var harness = new InMemoryFoodHubHarness();
        var restaurant = new Restaurant("Com Tam", Guid.NewGuid());
        var item = new MenuItem(restaurant.Id, "Suon bi", new Money(45000));
        harness.Store.AddRestaurant(restaurant);
        harness.Store.AddMenuItem(item);

        var created = await harness.OrderService.CreateOrderAsync(
            Guid.NewGuid(),
            new CreateOrderRequest(restaurant.Id, DefaultAddress, new[] { new OrderItemRequest(item.Id, 1) }));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            harness.OrderService.AcceptOrderAsync(Guid.NewGuid(), created.Id));
    }

    [Fact]
    public async Task GetOrderById_ShowsSixDigitCodeOnlyToCustomerDuringDelivery()
    {
        var harness = new InMemoryFoodHubHarness();
        var ownerId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var shipperUserId = Guid.NewGuid();
        var restaurant = new Restaurant("Com Tam", ownerId);
        var item = new MenuItem(restaurant.Id, "Suon bi", new Money(45000));
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
        await harness.DeliveryService.PickUpOrderAsync(shipperUserId, created.Id);

        var customerView = await harness.OrderService.GetOrderByIdAsync(customerId, UserRole.Customer, created.Id);
        var shipperView = await harness.OrderService.GetOrderByIdAsync(shipperUserId, UserRole.Shipper, created.Id);
        var ownerView = await harness.OrderService.GetOrderByIdAsync(ownerId, UserRole.RestaurantOwner, created.Id);

        Assert.NotNull(customerView.DeliveryConfirmationCode);
        Assert.Equal(6, customerView.DeliveryConfirmationCode!.Length);
        Assert.Null(shipperView.DeliveryConfirmationCode);
        Assert.Null(ownerView.DeliveryConfirmationCode);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            harness.OrderService.GetOrderByIdAsync(Guid.NewGuid(), UserRole.Customer, created.Id));
    }
}
