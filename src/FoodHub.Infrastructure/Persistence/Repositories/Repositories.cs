using FoodHub.Application.Abstractions;
using FoodHub.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.AnyAsync(x => x.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await db.Users.AddAsync(user, ct);
}

public class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await db.RefreshTokens.AddAsync(token, ct);

    public async Task RevokeAllActiveAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default)
    {
        var active = await db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ToListAsync(ct);
        foreach (var token in active) token.Revoke(nowUtc);
    }
}

public class RestaurantRepository(AppDbContext db) : IRestaurantRepository
{
    public Task<Restaurant?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Restaurants.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Restaurant>> GetAllAsync(CancellationToken ct = default) =>
        await db.Restaurants.OrderBy(x => x.Name).ToListAsync(ct);

    public Task<Restaurant?> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken ct = default) =>
        db.Restaurants.FirstOrDefaultAsync(x => x.OwnerUserId == ownerUserId, ct);

    public async Task AddAsync(Restaurant restaurant, CancellationToken ct = default) =>
        await db.Restaurants.AddAsync(restaurant, ct);
}

public class MenuItemRepository(AppDbContext db) : IMenuItemRepository
{
    public Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.MenuItems.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<MenuItem>> GetByRestaurantIdAsync(Guid restaurantId, bool availableOnly, CancellationToken ct = default)
    {
        var query = db.MenuItems.Where(x => x.RestaurantId == restaurantId);
        if (availableOnly)
            query = query.Where(x => x.IsAvailable);

        return await query.OrderBy(x => x.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MenuItem>> GetByIdsAsync(IEnumerable<Guid> menuItemIds, CancellationToken ct = default)
    {
        var ids = menuItemIds.Distinct().ToList();
        return await db.MenuItems.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
    }

    public async Task AddAsync(MenuItem item, CancellationToken ct = default) =>
        await db.MenuItems.AddAsync(item, ct);

    public void Remove(MenuItem item) =>
        db.MenuItems.Remove(item);
}

public class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        db.Orders.FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetAvailableForDeliveryAsync(CancellationToken cancellationToken = default) =>
        await db.Orders.Where(x => x.Status == OrderStatus.ReadyForPickup && x.ShipperId == null)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default) =>
        await db.Orders.AddAsync(order, cancellationToken);
}

public class DeliveryRepository(AppDbContext db) : IDeliveryRepository
{
    public Task<Delivery?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        db.Deliveries.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);

    public async Task AddAsync(Delivery delivery, CancellationToken cancellationToken = default) =>
        await db.Deliveries.AddAsync(delivery, cancellationToken);
}

public class ShipperRepository(AppDbContext db) : IShipperRepository
{
    public Task<Shipper?> GetByIdAsync(Guid shipperId, CancellationToken cancellationToken = default) =>
        db.Shippers.FirstOrDefaultAsync(x => x.Id == shipperId, cancellationToken);

    public async Task<IReadOnlyList<Shipper>> GetAvailableShippersAsync(CancellationToken cancellationToken = default) =>
        await db.Shippers.Where(x => x.IsAvailable).ToListAsync(cancellationToken);

    public Task SaveWithConcurrencyAsync(Shipper shipper, int expectedVersion, CancellationToken cancellationToken = default)
    {
        if (shipper.Version != expectedVersion)
            throw new ConcurrencyException("Shipper concurrency conflict.");
        return Task.CompletedTask;
    }

    public Task<Shipper?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Shippers.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task AddAsync(Shipper shipper, CancellationToken ct = default) =>
        await db.Shippers.AddAsync(shipper, ct);
}
