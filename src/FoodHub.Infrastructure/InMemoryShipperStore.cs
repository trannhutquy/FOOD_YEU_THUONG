using System.Collections.Concurrent;
using FoodHub.Domain;

namespace FoodHub.Infrastructure;

/// <summary>
/// In-memory implementation of IShipperStore supporting optimistic concurrency.
/// </summary>
public class InMemoryShipperStore : IShipperStore
{
    private readonly ConcurrentDictionary<Guid, Shipper> _shippers = new();

    public InMemoryShipperStore(IEnumerable<Shipper>? initialShippers = null)
    {
        if (initialShippers != null)
        {
            foreach (var shipper in initialShippers)
            {
                _shippers[shipper.Id] = shipper;
            }
        }
    }

    public Task<Shipper?> GetByIdAsync(Guid shipperId, CancellationToken cancellationToken = default)
    {
        _shippers.TryGetValue(shipperId, out var shipper);
        return Task.FromResult(shipper);
    }

    public Task<IReadOnlyList<Shipper>> GetAvailableShippersAsync(CancellationToken cancellationToken = default)
    {
        var available = _shippers.Values.Where(s => s.CanAcceptOrder()).ToList();
        return Task.FromResult<IReadOnlyList<Shipper>>(available);
    }

    public Task SaveWithConcurrencyAsync(Shipper shipper, int expectedVersion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shipper);

        if (!_shippers.TryGetValue(shipper.Id, out var existing))
        {
            _shippers[shipper.Id] = shipper;
            return Task.CompletedTask;
        }

        if (existing.Version != expectedVersion)
        {
            throw new ConcurrencyException(
                $"Shipper {shipper.Id} was modified by another process. Expected version {expectedVersion}, but found {existing.Version}.");
        }

        _shippers[shipper.Id] = shipper;
        return Task.CompletedTask;
    }
}
