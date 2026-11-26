namespace FoodHub.Domain;

/// <summary>
/// Domain service that handles concurrent shipper acceptance and automatic shipper assignment when an order is ready.
/// </summary>
public class AutoAssignService
{
    private const int MaxConcurrencyRetries = 3;

    private readonly IShipperStore _shipperStore;
    private readonly IShipperSelectionStrategy _selectionStrategy;

    public AutoAssignService(IShipperStore shipperStore, IShipperSelectionStrategy? selectionStrategy = null)
    {
        ArgumentNullException.ThrowIfNull(shipperStore);
        _shipperStore = shipperStore;
        _selectionStrategy = selectionStrategy ?? new LeastActiveOrdersShipperStrategy();
    }

    public async Task<Delivery> AcceptOrderAsync(
        Order order,
        Guid shipperId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        var shipper = await _shipperStore.GetByIdAsync(shipperId, cancellationToken)
            ?? throw new DomainException("Shipper not found.");

        var expectedVersion = shipper.Version;
        var delivery = Delivery.Assign(order, shipper, utcNow);

        try
        {
            await _shipperStore.SaveWithConcurrencyAsync(shipper, expectedVersion, cancellationToken);
            return delivery;
        }
        catch (ConcurrencyException)
        {
            order.UnassignShipper();
            throw;
        }
    }

    public async Task<Delivery?> MarkReadyWithAutoAssignAsync(
        Order order,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Status != OrderStatus.Preparing)
            throw new DomainException($"Cannot change order status from {order.Status} to {OrderStatus.ReadyForPickup}.");

        if (order.ShipperId.HasValue)
        {
            order.MarkReady();
            return null;
        }

        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            var candidates = await _shipperStore.GetAvailableShippersAsync(cancellationToken);
            var selected = _selectionStrategy.SelectShipper(candidates);

            if (selected is null)
                throw new DomainException("No available shipper can take this order right now.");

            var expectedVersion = selected.Version;
            var delivery = Delivery.Assign(order, selected, utcNow);

            try
            {
                await _shipperStore.SaveWithConcurrencyAsync(selected, expectedVersion, cancellationToken);
                order.MarkReady();
                return delivery;
            }
            catch (ConcurrencyException)
            {
                order.UnassignShipper();
                if (attempt == MaxConcurrencyRetries - 1)
                    throw;
            }
        }

        throw new DomainException("Unable to assign a shipper due to concurrent updates.");
    }
}
