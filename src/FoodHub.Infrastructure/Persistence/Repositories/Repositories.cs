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

    public async Task AddAsync(MenuItem item, CancellationToken ct = default) =>
        await db.MenuItems.AddAsync(item, ct);

    public void Remove(MenuItem item) =>
        db.MenuItems.Remove(item);
}

public class VoucherRepository(AppDbContext db) : IVoucherRepository
{
    public Task<Voucher?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return db.Vouchers
            .Include(x => x.Redemptions)
            .FirstOrDefaultAsync(x => x.Code == normalized, ct);
    }

    public Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return db.Vouchers.AnyAsync(x => x.Code == normalized, ct);
    }

    public async Task<IReadOnlyList<Voucher>> GetAllAsync(CancellationToken ct = default) =>
        await db.Vouchers.OrderByDescending(x => x.ExpiresAtUtc).ToListAsync(ct);

    public async Task AddAsync(Voucher voucher, CancellationToken ct = default) =>
        await db.Vouchers.AddAsync(voucher, ct);
}

public class ShipperRepository(AppDbContext db) : IShipperRepository
{
    public async Task AddAsync(Shipper shipper, CancellationToken ct = default) =>
        await db.Shippers.AddAsync(shipper, ct);
}
