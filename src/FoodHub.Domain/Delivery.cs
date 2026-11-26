namespace FoodHub.Domain;

/// <summary>
/// Tracks the assignment, pickup, confirmation code, and completion of an order delivery by a shipper.
/// </summary>
public class Delivery
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid ShipperId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }
    public DateTime? PickedUpAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public DeliveryCode? Code { get; private set; }

    private Delivery() { }

    public static Delivery Assign(Order order, Shipper shipper, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(shipper);

        shipper.AcceptOrder(order);

        return new Delivery
        {
            OrderId = order.Id,
            ShipperId = shipper.Id,
            AssignedAtUtc = utcNow
        };
    }

    public string StartDelivery(Order order, DateTime utcNow, TimeSpan? codeTtl = null)
    {
        ArgumentNullException.ThrowIfNull(order);
        EnsureMatchesOrder(order);

        order.PickUp(ShipperId);
        var (code, plainCode) = DeliveryCode.Create(utcNow, codeTtl);
        PickedUpAtUtc = utcNow;
        Code = code;

        return plainCode;
    }

    public void ConfirmDelivery(Order order, Shipper shipper, string candidateCode, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(shipper);
        EnsureMatchesOrder(order);

        if (shipper.Id != ShipperId)
            throw new DomainException("Only the assigned shipper can confirm delivery.");
        if (Code is null)
            throw new DomainException("Delivery code has not been generated yet.");

        Code.VerifyOrThrow(candidateCode, utcNow);
        order.MarkDelivered();
        DeliveredAtUtc = utcNow;
        shipper.ReleaseOrder(order);
    }

    public string RegenerateCode(DateTime utcNow, TimeSpan? codeTtl = null)
    {
        if (DeliveredAtUtc.HasValue)
            throw new DomainException("Cannot regenerate code for a completed delivery.");
        if (Code is null)
            throw new DomainException("Delivery has not started yet.");

        return Code.Reset(utcNow, codeTtl);
    }

    public void CancelDelivery(Order order, Shipper shipper, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(shipper);
        EnsureMatchesOrder(order);

        if (shipper.Id != ShipperId)
            throw new DomainException("Order is not assigned to this shipper.");

        order.Cancel(reason);
        shipper.ReleaseOrder(order);
    }

    private void EnsureMatchesOrder(Order order)
    {
        if (order.Id != OrderId)
            throw new DomainException("Order does not match this delivery.");
    }
}
