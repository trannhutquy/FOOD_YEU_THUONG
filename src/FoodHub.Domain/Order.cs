namespace FoodHub.Domain;

public class Order
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Accepted, OrderStatus.Rejected, OrderStatus.Cancelled },
        [OrderStatus.Accepted] = new[] { OrderStatus.Preparing, OrderStatus.Cancelled },
        [OrderStatus.Preparing] = new[] { OrderStatus.ReadyForPickup },
        [OrderStatus.ReadyForPickup] = new[] { OrderStatus.Delivering },
        [OrderStatus.Delivering] = new[] { OrderStatus.Delivered },
    };

    private readonly List<OrderItem> _items = new();

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CustomerId { get; private set; }
    public Guid RestaurantId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public Guid? ShipperId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public IReadOnlyList<OrderItem> Items => _items;
    public Money Total => _items.Aggregate(Money.Zero, (sum, item) => sum + item.LineTotal);

    private Order() { }

    public static Order Create(Guid customerId, Restaurant restaurant)
    {
        if (!restaurant.IsOpen) throw new DomainException("Restaurant is closed.");
        return new Order { CustomerId = customerId, RestaurantId = restaurant.Id };
    }

    public void AddItem(MenuItem item, int quantity)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Items can only be changed while the order is pending.");
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");
        if (item.RestaurantId != RestaurantId)
            throw new DomainException("An order can only contain items from one restaurant.");
        if (!item.IsAvailable)
            throw new DomainException("Menu item is not available.");

        var existing = _items.FirstOrDefault(i => i.MenuItemId == item.Id);
        if (existing is null) _items.Add(new OrderItem(item.Id, item.Name, item.Price, quantity));
        else existing.IncreaseQuantity(quantity);
    }

    public void Accept() => Transition(OrderStatus.Accepted);
    public void Reject() => Transition(OrderStatus.Rejected);
    public void Cancel() => Transition(OrderStatus.Cancelled);
    public void StartPreparing() => Transition(OrderStatus.Preparing);
    public void MarkReady() => Transition(OrderStatus.ReadyForPickup);
    public void MarkDelivered() => Transition(OrderStatus.Delivered);

    public void AssignShipper(Guid shipperId)
    {
        if (ShipperId is not null)
            throw new DomainException("Order already has a shipper.");
        if (Status is not (OrderStatus.Accepted or OrderStatus.Preparing or OrderStatus.ReadyForPickup))
            throw new DomainException("Order is not ready to be assigned to a shipper.");
        ShipperId = shipperId;
    }

    public void PickUp(Guid shipperId)
    {
        if (ShipperId != shipperId)
            throw new DomainException("Only the assigned shipper can pick up this order.");
        Transition(OrderStatus.Delivering);
    }

    private void Transition(OrderStatus next)
    {
        if (!Allowed.TryGetValue(Status, out var targets) || !targets.Contains(next))
            throw new DomainException($"Cannot change order status from {Status} to {next}.");
        Status = next;
    }
}