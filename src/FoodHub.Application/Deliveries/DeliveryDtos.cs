using FoodHub.Application.Orders;
using FoodHub.Domain;

namespace FoodHub.Application.Deliveries;

public sealed record UpdateAvailabilityRequest(bool IsAvailable);

public sealed record ConfirmDeliveryRequest(string Code);

public sealed record AvailableOrderDto(
    Guid OrderId,
    Guid RestaurantId,
    OrderStatus Status,
    AddressDto? DeliveryAddress,
    decimal Total,
    DateTime CreatedAtUtc);

public sealed record DeliveryStatusDto(
    Guid DeliveryId,
    Guid OrderId,
    Guid ShipperId,
    OrderStatus OrderStatus,
    DateTime AssignedAtUtc,
    DateTime? PickedUpAtUtc,
    DateTime? DeliveredAtUtc,
    bool IsCodeLocked,
    int CodeFailedAttempts);
