using FoodHub.Domain;

namespace FoodHub.Application.Abstractions;

public interface IRestaurantRepository
{
    Task<Restaurant?> GetByIdAsync(Guid restaurantId, CancellationToken cancellationToken = default);
}

public interface IMenuItemRepository
{
    Task<IReadOnlyList<MenuItem>> GetByIdsAsync(IEnumerable<Guid> menuItemIds, CancellationToken cancellationToken = default);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetAvailableForDeliveryAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
}

public interface IDeliveryRepository
{
    Task<Delivery?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task AddAsync(Delivery delivery, CancellationToken cancellationToken = default);
}

public interface IShipperRepository : IShipperStore
{
    Task<Shipper?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Shipper shipper, CancellationToken cancellationToken = default);
}

/// <summary>
/// Integration point for Vy's Voucher module (Week 11).
/// </summary>
public interface IVoucherEvaluator
{
    Task<IDiscountStrategy> ResolveDiscountAsync(
        Guid customerId,
        string voucherCode,
        Money subtotal,
        CancellationToken cancellationToken = default);
}
