namespace FoodHub.Domain;

/// <summary>
/// Represents a delivery driver who can hold at most 2 active orders concurrently.
/// Includes a concurrency version token to guard against parallel over-assignment.
/// </summary>
public class Shipper
{
    public const int MaxActiveOrders = 2;

    private readonly List<Guid> _activeOrderIds = new();

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public bool IsAvailable { get; private set; }
    public int Version { get; private set; } = 1;
    public IReadOnlyList<Guid> ActiveOrderIds => _activeOrderIds;
    public int ActiveOrderCount => _activeOrderIds.Count;

    private Shipper() { }

    public Shipper(Guid userId, bool isAvailable = true)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User is required.");

        UserId = userId;
        IsAvailable = isAvailable;
    }

    public static Shipper Restore(
        Guid id,
        Guid userId,
        bool isAvailable,
        int version,
        IEnumerable<Guid> activeOrderIds)
    {
        if (id == Guid.Empty)
            throw new DomainException("Shipper id is required.");
        if (userId == Guid.Empty)
            throw new DomainException("User is required.");

        var shipper = new Shipper
        {
            Id = id,
            UserId = userId,
            IsAvailable = isAvailable,
            Version = version
        };
        shipper._activeOrderIds.AddRange(activeOrderIds);
        return shipper;
    }

    public void SetAvailability(bool isAvailable)
    {
        if (IsAvailable == isAvailable)
            return;

        IsAvailable = isAvailable;
        Version++;
    }

    public bool CanAcceptOrder() => IsAvailable && _activeOrderIds.Count < MaxActiveOrders;

    public void AcceptOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (!IsAvailable)
            throw new DomainException("Shipper is not available.");
        if (_activeOrderIds.Contains(order.Id))
            throw new DomainException("Shipper already holds this order.");
        if (_activeOrderIds.Count >= MaxActiveOrders)
            throw new DomainException($"Shipper cannot hold more than {MaxActiveOrders} active orders.");

        order.AssignShipper(Id);
        _activeOrderIds.Add(order.Id);
        Version++;
    }

    public void ReleaseOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Status is not (OrderStatus.Delivered or OrderStatus.Cancelled))
            throw new DomainException("Only delivered or cancelled orders can be released.");

        if (_activeOrderIds.Remove(order.Id))
            Version++;
    }
}
