using FoodHub.Application.Abstractions;
using FoodHub.Domain;

namespace FoodHub.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _tokens;
    private readonly IShipperRepository _shippers;
    private readonly IRestaurantRepository _restaurants;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository users, IRefreshTokenRepository tokens,
        IShipperRepository shippers, IRestaurantRepository restaurants,
        IPasswordHasher hasher, ITokenService tokenService, IUnitOfWork unitOfWork)
    {
        _users = users; _tokens = tokens; _shippers = shippers; _restaurants = restaurants;
        _hasher = hasher; _tokenService = tokenService; _unitOfWork = unitOfWork;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        // 1. Kiểm tra đầu vào trước khi chạm vào cơ sở dữ liệu
        if (request.Role == UserRole.Admin)
            throw new ForbiddenException("Cannot register an admin account.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new DomainException("Email is required.");
        if (request.Password is null || request.Password.Length < 8)
            throw new DomainException("Password must be at least 8 characters.");
        if (request.Role == UserRole.Restaurant && string.IsNullOrWhiteSpace(request.RestaurantName))
            throw new DomainException("Restaurant name is required.");

        // 2. Email trùng (không phân biệt hoa thường) -> 409
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.ExistsByEmailAsync(email, ct))
            throw new ConflictException("Email is already registered.");

        // 3. Tạo User; mật khẩu chỉ lưu bản băm
        var user = new User(email, _hasher.Hash(request.Password), request.FullName, request.Role);
        await _users.AddAsync(user, ct);

        // 4. Vai trò nào tạo luôn bản ghi đó
        if (request.Role == UserRole.Shipper)
            await _shippers.AddAsync(new Shipper(user.Id), ct);
        else if (request.Role == UserRole.Restaurant)
            await _restaurants.AddAsync(new Restaurant(request.RestaurantName!.Trim(), user.Id), ct);

        var result = await IssueTokensAsync(user, ct);

        // 5. MỘT lần lưu duy nhất: User + Shipper/Restaurant + refresh token cùng thành công hoặc cùng thất bại
        await _unitOfWork.SaveChangesAsync(ct);
        return result;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct);

        // Sai email hay sai mật khẩu đều cùng một thông báo, để không dò được email nào đã đăng ký
        if (user is null || !_hasher.Verify(request.Password ?? "", user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        var result = await IssueTokensAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return result;
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = _tokenService.HashRefreshToken(refreshToken ?? "");
        var stored = await _tokens.GetByHashAsync(hash, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        var now = DateTime.UtcNow;

        if (stored.IsRevoked)
        {
            // Token đã bị thu hồi mà vẫn bị dùng lại: nghi bị đánh cắp -> thu hồi mọi phiên của người này
            await _tokens.RevokeAllActiveAsync(stored.UserId, now, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            throw new UnauthorizedException("Refresh token was already used.");
        }

        if (stored.IsExpired(now))
            throw new UnauthorizedException("Refresh token expired.");

        var user = await _users.GetByIdAsync(stored.UserId, ct)
            ?? throw new UnauthorizedException("User no longer exists.");

        // Xoay vòng: thu hồi token cũ, cấp token mới
        var (newPlain, newEntity) = _tokenService.CreateRefreshToken(user.Id);
        stored.Revoke(now, newEntity.TokenHash);
        await _tokens.AddAsync(newEntity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new AuthResult(_tokenService.CreateAccessToken(user), newPlain);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await _tokens.GetByHashAsync(_tokenService.HashRefreshToken(refreshToken ?? ""), ct);
        if (stored is null || stored.IsRevoked) return;   // đăng xuất nhiều lần vẫn êm
        stored.Revoke(DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // Tạo access token + refresh token mới (chưa lưu; người gọi lưu)
    private async Task<AuthResult> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (plain, entity) = _tokenService.CreateRefreshToken(user.Id);
        await _tokens.AddAsync(entity, ct);
        return new AuthResult(_tokenService.CreateAccessToken(user), plain);
    }
}
