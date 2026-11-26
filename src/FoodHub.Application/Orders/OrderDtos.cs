using FoodHub.Domain;

namespace FoodHub.Application.Orders;

public sealed record AddressDto(
    string Street,
    string Ward,
    string District,
    string City,
    string? Note = null);

public sealed record OrderItemRequest(Guid MenuItemId, int Quantity);

public sealed record CreateOrderRequest(
    Guid RestaurantId,
    AddressDto DeliveryAddress,
    IReadOnlyList<OrderItemRequest> Items,
    string? VoucherCode = null,
    decimal ShippingFee = 0);

public sealed record CancelOrderRequest(string? Reason = null);

public sealed record RejectOrderRequest(string Reason);

public sealed record OrderItemDto(
    Guid MenuItemId,
    string Name,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record OrderDetailsDto(
    Guid Id,
    Guid CustomerId,
    Guid RestaurantId,
    OrderStatus Status,
    Guid? ShipperId,
    AddressDto? DeliveryAddress,
    IReadOnlyList<OrderItemDto> Items,
    decimal Subtotal,
    decimal ShippingFee,
    decimal DiscountAmount,
    decimal Total,
    string? AppliedVoucherCode,
    string? RejectionReason,
    string? CancellationReason,
    string? DeliveryConfirmationCode,
    DateTime CreatedAtUtc);
