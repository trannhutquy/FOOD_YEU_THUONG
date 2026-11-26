using FoodHub.Domain;
using Xunit;

namespace FoodHub.Domain.Tests;

public class AutoAssignServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 26, 10, 0, 0, DateTimeKind.Utc);

    private sealed class InMemoryShipperStore : IShipperStore
    {
        private readonly object _lock = new();
        private readonly Dictionary<Guid, Shipper> _shippers = new();

        public void Seed(params Shipper[] shippers)
        {
            lock (_lock)
            {
                foreach (var s in shippers)
                    _shippers[s.Id] = Clone(s);
            }
        }

        public Shipper GetStoredSnapshot(Guid id)
        {
            lock (_lock)
            {
                return Clone(_shippers[id]);
            }
        }

        public Task<Shipper?> GetByIdAsync(Guid shipperId, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                return Task.FromResult(_shippers.TryGetValue(shipperId, out var s) ? Clone(s) : null);
            }
        }

        public Task<IReadOnlyList<Shipper>> GetAvailableShippersAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                IReadOnlyList<Shipper> list = _shippers.Values
                    .Where(s => s.CanAcceptOrder())
                    .Select(Clone)
                    .ToList();
                return Task.FromResult(list);
            }
        }

        public Task SaveWithConcurrencyAsync(Shipper shipper, int expectedVersion, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_shippers.TryGetValue(shipper.Id, out var current))
                    throw new DomainException("Shipper not found.");

                if (current.Version != expectedVersion)
                    throw new ConcurrencyException("Shipper was updated concurrently.");

                _shippers[shipper.Id] = Clone(shipper);
                return Task.CompletedTask;
            }
        }

        private static Shipper Clone(Shipper source)
            => Shipper.Restore(source.Id, source.UserId, source.IsAvailable, source.Version, source.ActiveOrderIds);
    }

    private sealed class BarrierShipperStore : IShipperStore
    {
        private readonly InMemoryShipperStore _inner = new();
        private readonly TaskCompletionSource _bothLoaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _loadCount;

        public void Seed(params Shipper[] shippers) => _inner.Seed(shippers);
        public Shipper GetStoredSnapshot(Guid id) => _inner.GetStoredSnapshot(id);

        public async Task<Shipper?> GetByIdAsync(Guid shipperId, CancellationToken cancellationToken = default)
        {
            var result = await _inner.GetByIdAsync(shipperId, cancellationToken);
            if (Interlocked.Increment(ref _loadCount) == 2)
                _bothLoaded.TrySetResult();
            await _bothLoaded.Task;
            return result;
        }

        public Task<IReadOnlyList<Shipper>> GetAvailableShippersAsync(CancellationToken cancellationToken = default)
            => _inner.GetAvailableShippersAsync(cancellationToken);

        public Task SaveWithConcurrencyAsync(Shipper shipper, int expectedVersion, CancellationToken cancellationToken = default)
            => _inner.SaveWithConcurrencyAsync(shipper, expectedVersion, cancellationToken);
    }

    private static Order NewPreparingOrder()
    {
        var restaurant = new Restaurant("Com Tam", Guid.NewGuid());
        var item = new MenuItem(restaurant.Id, "Suon bi cha", new Money(45000));
        var order = Order.Create(Guid.NewGuid(), restaurant);
        order.AddItem(item, 1);
        order.Accept();
        order.StartPreparing();
        return order;
    }

    private static Order NewAcceptedOrder()
    {
        var restaurant = new Restaurant("Com Tam", Guid.NewGuid());
        var item = new MenuItem(restaurant.Id, "Suon bi cha", new Money(45000));
        var order = Order.Create(Guid.NewGuid(), restaurant);
        order.AddItem(item, 1);
        order.Accept();
        return order;
    }

    [Fact]
    public async Task MarkReadyWithAutoAssign_SelectsAvailableShipperWithFewestActiveOrders()
    {
        var store = new InMemoryShipperStore();
        var offline = new Shipper(Guid.NewGuid(), isAvailable: false);
        var busy = new Shipper(Guid.NewGuid());
        busy.AcceptOrder(NewAcceptedOrder());
        var idle = new Shipper(Guid.NewGuid());

        store.Seed(offline, busy, idle);
        var service = new AutoAssignService(store);
        var order = NewPreparingOrder();

        var delivery = await service.MarkReadyWithAutoAssignAsync(order, Now);

        Assert.NotNull(delivery);
        Assert.Equal(OrderStatus.ReadyForPickup, order.Status);
        Assert.Equal(idle.Id, order.ShipperId);
        Assert.Equal(1, store.GetStoredSnapshot(idle.Id).ActiveOrderCount);
    }

    [Fact]
    public async Task MarkReadyWithAutoAssign_WhenNoShipperAvailable_ThrowsAndKeepsOrderPreparing()
    {
        var store = new InMemoryShipperStore();
        var full = new Shipper(Guid.NewGuid());
        full.AcceptOrder(NewAcceptedOrder());
        full.AcceptOrder(NewAcceptedOrder());
        var offline = new Shipper(Guid.NewGuid(), isAvailable: false);

        store.Seed(full, offline);
        var service = new AutoAssignService(store);
        var order = NewPreparingOrder();

        await Assert.ThrowsAsync<DomainException>(() => service.MarkReadyWithAutoAssignAsync(order, Now));
        Assert.Equal(OrderStatus.Preparing, order.Status);
        Assert.Null(order.ShipperId);
    }

    [Fact]
    public async Task MarkReadyWithAutoAssign_WhenOrderAlreadyHasShipper_MarksReadyWithoutReassigning()
    {
        var store = new InMemoryShipperStore();
        var preAssigned = new Shipper(Guid.NewGuid());
        var otherIdle = new Shipper(Guid.NewGuid());
        var order = NewAcceptedOrder();
        preAssigned.AcceptOrder(order);
        order.StartPreparing();

        store.Seed(preAssigned, otherIdle);
        var service = new AutoAssignService(store);

        var delivery = await service.MarkReadyWithAutoAssignAsync(order, Now);

        Assert.Null(delivery);
        Assert.Equal(OrderStatus.ReadyForPickup, order.Status);
        Assert.Equal(preAssigned.Id, order.ShipperId);
        Assert.Equal(0, store.GetStoredSnapshot(otherIdle.Id).ActiveOrderCount);
    }

    [Fact]
    public async Task AcceptOrder_TwoConcurrentRequestsForLastSlot_OnlyOneSucceedsAndNeverExceedsTwo()
    {
        var store = new BarrierShipperStore();
        var shipper = new Shipper(Guid.NewGuid());
        shipper.AcceptOrder(NewAcceptedOrder());
        store.Seed(shipper);

        var service = new AutoAssignService(store);
        var order1 = NewAcceptedOrder();
        var order2 = NewAcceptedOrder();

        var t1 = Task.Run(async () =>
        {
            try
            {
                await service.AcceptOrderAsync(order1, shipper.Id, Now);
                return (Success: true, Ex: (Exception?)null);
            }
            catch (Exception ex)
            {
                return (Success: false, Ex: ex);
            }
        });

        var t2 = Task.Run(async () =>
        {
            try
            {
                await service.AcceptOrderAsync(order2, shipper.Id, Now);
                return (Success: true, Ex: (Exception?)null);
            }
            catch (Exception ex)
            {
                return (Success: false, Ex: ex);
            }
        });

        var results = await Task.WhenAll(t1, t2);

        Assert.Equal(1, results.Count(r => r.Success));
        var failure = results.Single(r => !r.Success);
        Assert.IsType<ConcurrencyException>(failure.Ex);

        var finalShipper = store.GetStoredSnapshot(shipper.Id);
        Assert.Equal(2, finalShipper.ActiveOrderCount);

        var unassignedOrder = results[0].Success ? order2 : order1;
        Assert.Null(unassignedOrder.ShipperId);
    }
}
