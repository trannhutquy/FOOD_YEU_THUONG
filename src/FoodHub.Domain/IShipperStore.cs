namespace FoodHub.Domain;

/// <summary>
/// Domain persistence port for loading shippers and saving updates with optimistic concurrency.
/// </summary>
public interface IShipperStore
{
    Task<Shipper?> GetByIdAsync(Guid shipperId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shipper>> GetAvailableShippersAsync(CancellationToken cancellationToken = default);
    Task SaveWithConcurrencyAsync(Shipper shipper, int expectedVersion, CancellationToken cancellationToken = default);
}
