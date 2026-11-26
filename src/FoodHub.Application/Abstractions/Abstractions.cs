using FoodHub.Domain;

namespace FoodHub.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    string CreateAccessToken(User user);
    (string Plain, RefreshToken Entity) CreateRefreshToken(Guid userId);
    string HashRefreshToken(string plain);
}

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid Id { get; }
    UserRole Role { get; }
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeAllActiveAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default);
}

public interface IRestaurantRepository
{
    Task<Restaurant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Restaurant>> GetAllAsync(CancellationToken ct = default);
    Task<Restaurant?> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken ct = default);
    Task AddAsync(Restaurant restaurant, CancellationToken ct = default);
}

public interface IMenuItemRepository
{
    Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MenuItem>> GetByRestaurantIdAsync(Guid restaurantId, bool availableOnly, CancellationToken ct = default);
    Task AddAsync(MenuItem item, CancellationToken ct = default);
    void Remove(MenuItem item);
}

public interface IVoucherRepository
{
    Task<Voucher?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Voucher>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Voucher voucher, CancellationToken ct = default);
}

// Chỉ để TẠO Shipper lúc đăng ký. Việc nhận/giải phóng đơn có đồng thời vẫn là IShipperStore của Quy.
public interface IShipperRepository
{
    Task AddAsync(Shipper shipper, CancellationToken ct = default);
}
