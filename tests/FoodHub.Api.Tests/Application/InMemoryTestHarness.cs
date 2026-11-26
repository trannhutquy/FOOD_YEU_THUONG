using FoodHub.Application.Abstractions;
using FoodHub.Application.Deliveries;
using FoodHub.Application.Orders;
using FoodHub.Domain;

namespace FoodHub.Api.Tests.Application;

internal sealed class FakeClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; set; } = utcNow;
}

internal sealed class FakeCodeProtector : IDeliveryCodeProtector
{
    private const string Prefix = "ENC:";
    public string Protect(string plainCode) => Prefix + plainCode;
    public string Unprotect(string protectedCode)
        => protectedCode.StartsWith(Prefix, StringComparison.Ordinal)
            ? protectedCode[Prefix.Length..]
            : throw new InvalidOperationException("Invalid protected code.");
}

internal sealed class InMemoryFoodHubHarness
{
    public FakeClock Clock { get; } = new(new DateTime(2026, 11, 26, 11, 0, 0, DateTimeKind.Utc));
    public FakeCodeProtector Protector { get; } = new();
    public InMemoryStore Store { get; } = new();
    public AutoAssignService AutoAssign { get; }
    public OrderService OrderService { get; }
    public DeliveryService DeliveryService { get; }

    public InMemoryFoodHubHarness()
    {
        AutoAssign = new AutoAssignService(Store);
        OrderService = new OrderService(
            Store,
            Store,
            Store,
            Store,
            Store,
            Protector,
            AutoAssign,
            Clock,
            Store);

        DeliveryService = new DeliveryService(
            Store,
            Store,
            Store,
            Store,
            Protector,
            AutoAssign,
            Clock,
            Store);
    }
}

internal sealed class InMemoryStore :
    IRestaurantRepository,
    IMenuItemRepository,
    IOrderRepository,
    IDeliveryRepository,
    IShipperRepository,
    IUnitOfWork
{
    private readonly Dictionary<Guid, Restaurant> _restaurants = new();
    private readonly Dictionary<Guid, MenuItem> _menuItems = new();
    private readonly Dictionary<Guid, Order> _orders = new();
    private readonly Dictionary<Guid, Delivery> _deliveriesByOrder = new();
    private readonly Dictionary<Guid, Shipper> _shippers = new();

    public int SaveCount { get; private set; }

    public void AddRestaurant(Restaurant restaurant) => _restaurants[restaurant.Id] = restaurant;
    public void AddMenuItem(MenuItem item) => _menuItems[item.Id] = item;

    public Task<Restaurant?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_restaurants.GetValueOrDefault(id));

    public Task<IReadOnlyList<Restaurant>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Restaurant>>(_restaurants.Values.ToList());

    public Task<Restaurant?> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken ct = default)
        => Task.FromResult(_restaurants.Values.FirstOrDefault(x => x.OwnerUserId == ownerUserId));

    public Task AddAsync(Restaurant restaurant, CancellationToken ct = default)
    {
        _restaurants[restaurant.Id] = restaurant;
        return Task.CompletedTask;
    }

    Task<MenuItem?> IMenuItemRepository.GetByIdAsync(Guid id, CancellationToken ct)
        => Task.FromResult(_menuItems.GetValueOrDefault(id));

    public Task<IReadOnlyList<MenuItem>> GetByRestaurantIdAsync(Guid restaurantId, bool availableOnly, CancellationToken ct = default)
    {
        var q = _menuItems.Values.Where(x => x.RestaurantId == restaurantId);
        if (availableOnly) q = q.Where(x => x.IsAvailable);
        return Task.FromResult<IReadOnlyList<MenuItem>>(q.ToList());
    }

    public Task<IReadOnlyList<MenuItem>> GetByIdsAsync(IEnumerable<Guid> menuItemIds, CancellationToken ct = default)
    {
        IReadOnlyList<MenuItem> result = menuItemIds
            .Where(_menuItems.ContainsKey)
            .Select(id => _menuItems[id])
            .ToList();
        return Task.FromResult(result);
    }

    Task IMenuItemRepository.AddAsync(MenuItem item, CancellationToken ct)
    {
        _menuItems[item.Id] = item;
        return Task.CompletedTask;
    }

    public void Remove(MenuItem item) => _menuItems.Remove(item.Id);

    Task<Order?> IOrderRepository.GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
        => Task.FromResult(_orders.GetValueOrDefault(orderId));

    public Task<IReadOnlyList<Order>> GetAvailableForDeliveryAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Order> result = _orders.Values
            .Where(o => o.ShipperId is null &&
                        o.Status is OrderStatus.Accepted or OrderStatus.Preparing or OrderStatus.ReadyForPickup)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }

    public Task<Delivery?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        => Task.FromResult(_deliveriesByOrder.GetValueOrDefault(orderId));

    public Task AddAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        _deliveriesByOrder[delivery.OrderId] = delivery;
        return Task.CompletedTask;
    }

    Task<Shipper?> IShipperStore.GetByIdAsync(Guid shipperId, CancellationToken cancellationToken)
        => Task.FromResult(_shippers.GetValueOrDefault(shipperId));

    public Task<Shipper?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(_shippers.Values.FirstOrDefault(s => s.UserId == userId));

    public Task AddAsync(Shipper shipper, CancellationToken cancellationToken = default)
    {
        _shippers[shipper.Id] = shipper;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Shipper>> GetAvailableShippersAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Shipper> result = _shippers.Values.Where(s => s.CanAcceptOrder()).ToList();
        return Task.FromResult(result);
    }

    public Task SaveWithConcurrencyAsync(Shipper shipper, int expectedVersion, CancellationToken cancellationToken = default)
    {
        if (!_shippers.ContainsKey(shipper.Id))
            throw new DomainException("Shipper not found.");

        _shippers[shipper.Id] = shipper;
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }

    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken, bool _)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
