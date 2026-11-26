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
    public Address? DeliveryAddress { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? AppliedVoucherCode { get; private set; }
    public Money ShippingFee { get; private set; } = Money.Zero;
    public Money DiscountAmount { get; private set; } = Money.Zero;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public bool IsReviewed { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items;
    public Money Subtotal => _items.Aggregate(Money.Zero, (sum, item) => sum + item.LineTotal);
    public Money Total => Subtotal + ShippingFee - DiscountAmount;

    private Order() { }

    public static Order Create(
        Guid customerId,
        Restaurant restaurant,
        Address? deliveryAddress = null,
        Money? shippingFee = null)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Customer is required.");
        ArgumentNullException.ThrowIfNull(restaurant);
        if (!restaurant.IsOpen)
            throw new DomainException("Restaurant is closed.");

        return new Order
        {
            CustomerId = customerId,
            RestaurantId = restaurant.Id,
            DeliveryAddress = deliveryAddress,
            ShippingFee = shippingFee ?? Money.Zero
        };
    }

    public void AddItem(MenuItem item, int quantity)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsurePending("Items can only be changed while the order is pending.");
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");
        if (item.RestaurantId != RestaurantId)
            throw new DomainException("An order can only contain items from one restaurant.");
        if (!item.IsAvailable)
            throw new DomainException("Menu item is not available.");

        var existing = _items.FirstOrDefault(i => i.MenuItemId == item.Id);
        if (existing is null)
            _items.Add(new OrderItem(item.Id, item.Name, item.Price, quantity));
        else
            existing.IncreaseQuantity(quantity);
    }

    public void UpdateItemQuantity(Guid menuItemId, int quantity)
    {
        EnsurePending("Items can only be changed while the order is pending.");
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        var existing = _items.FirstOrDefault(i => i.MenuItemId == menuItemId)
            ?? throw new DomainException("Order item not found.");

        existing.SetQuantity(quantity);
    }

    public void RemoveItem(Guid menuItemId)
    {
        EnsurePending("Items can only be changed while the order is pending.");
        var existing = _items.FirstOrDefault(i => i.MenuItemId == menuItemId)
            ?? throw new DomainException("Order item not found.");

        _items.Remove(existing);
    }

    public void ApplyDiscount(string voucherCode, IDiscountStrategy discountStrategy)
    {
        EnsurePending("Discount can only be applied while the order is pending.");
        if (string.IsNullOrWhiteSpace(voucherCode))
            throw new DomainException("Voucher code is required.");
        if (_items.Count == 0)
            throw new DomainException("Cannot apply discount to an empty order.");
        ArgumentNullException.ThrowIfNull(discountStrategy);

        var rawDiscount = discountStrategy.CalculateDiscount(Subtotal, ShippingFee);
        var maxDiscount = Subtotal + ShippingFee;
        DiscountAmount = Money.Min(rawDiscount, maxDiscount);
        AppliedVoucherCode = voucherCode.Trim();
    }

    public void Accept()
    {
        if (_items.Count == 0)
            throw new DomainException("Cannot accept an empty order.");
        Transition(OrderStatus.Accepted);
    }

    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rejection reason is required.");
        Transition(OrderStatus.Rejected);
        RejectionReason = reason.Trim();
    }

    public void Cancel(string? reason = null)
    {
        Transition(OrderStatus.Cancelled);
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public void StartPreparing() => Transition(OrderStatus.Preparing);
    public void MarkReady() => Transition(OrderStatus.ReadyForPickup);
    public void MarkDelivered() => Transition(OrderStatus.Delivered);

    public void MarkReviewed()
    {
        if (Status != OrderStatus.Delivered)
            throw new DomainException("Only delivered orders can be reviewed.");
        if (IsReviewed)
            throw new DomainException("Order has already been reviewed.");

        IsReviewed = true;
    }

    public Review AddReview(
        Guid customerId,
        int restaurantRating,
        string? restaurantComment = null,
        int? shipperRating = null,
        string? shipperComment = null,
        DateTime? utcNow = null)
    {
        return Review.Create(this, customerId, restaurantRating, restaurantComment, shipperRating, shipperComment, utcNow);
    }

    public static Order Restore(
        Guid id,
        Guid customerId,
        Guid restaurantId,
        OrderStatus status,
        Guid? shipperId = null,
        Address? deliveryAddress = null,
        Money? shippingFee = null,
        Money? discountAmount = null,
        string? appliedVoucherCode = null,
        bool isReviewed = false,
        DateTime? createdAtUtc = null,
        IEnumerable<OrderItem>? items = null)
    {
        if (id == Guid.Empty) throw new DomainException("Order id is required.");
        if (customerId == Guid.Empty) throw new DomainException("Customer is required.");
        if (restaurantId == Guid.Empty) throw new DomainException("Restaurant is required.");

        var order = new Order
        {
            Id = id,
            CustomerId = customerId,
            RestaurantId = restaurantId,
            Status = status,
            ShipperId = shipperId,
            DeliveryAddress = deliveryAddress,
            ShippingFee = shippingFee ?? Money.Zero,
            DiscountAmount = discountAmount ?? Money.Zero,
            AppliedVoucherCode = appliedVoucherCode,
            IsReviewed = isReviewed,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow
        };

        if (items is not null)
        {
            order._items.AddRange(items);
        }

        return order;
    }

    public void AssignShipper(Guid shipperId)
    {
        if (shipperId == Guid.Empty)
            throw new DomainException("Shipper is required.");
        if (ShipperId is not null)
            throw new DomainException("Order already has a shipper.");
        if (Status is not (OrderStatus.Accepted or OrderStatus.Preparing or OrderStatus.ReadyForPickup))
            throw new DomainException("Order is not ready to be assigned to a shipper.");

        ShipperId = shipperId;
    }

    internal void UnassignShipper()
    {
        ShipperId = null;
    }

    public void PickUp(Guid shipperId)
    {
        if (ShipperId != shipperId)
            throw new DomainException("Only the assigned shipper can pick up this order.");
        Transition(OrderStatus.Delivering);
    }

    private void EnsurePending(string message)
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException(message);
    }

    private void Transition(OrderStatus next)
    {
        if (!Allowed.TryGetValue(Status, out var targets) || !targets.Contains(next))
            throw new DomainException($"Cannot change order status from {Status} to {next}.");
        Status = next;
    }
}
