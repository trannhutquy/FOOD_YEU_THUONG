using FoodHub.Application.Abstractions;
using FoodHub.Application.Auth;
using FoodHub.Domain;
using Xunit;

namespace FoodHub.Application.Tests;

public class AuthServiceTests
{
    private static RegisterRequest Reg(UserRole role, string email = "a@mail.com",
        string password = "password1", string? restaurantName = null) =>
        new(email, password, "Nguyen A", role, restaurantName);

    [Fact]
    public async Task Register_Customer_CreatesUserAndReturnsTokens()
    {
        var f = new Fixture();

        var result = await f.Service.RegisterAsync(Reg(UserRole.Customer, "A@Mail.com"));

        var user = Assert.Single(f.Users.Items);
        Assert.Equal("a@mail.com", user.Email);                  // email được chuẩn hoá
        Assert.NotEqual("password1", user.PasswordHash);         // không lưu mật khẩu thô
        Assert.Empty(f.Shippers.Items);
        Assert.Empty(f.Restaurants.Items);
        Assert.StartsWith("access-", result.AccessToken);
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.Equal(1, f.Uow.SaveCount);                        // một lần lưu duy nhất
    }

    [Fact]
    public async Task Register_Shipper_AlsoCreatesShipperForThatUser()
    {
        var f = new Fixture();

        await f.Service.RegisterAsync(Reg(UserRole.Shipper));

        var user = Assert.Single(f.Users.Items);
        var shipper = Assert.Single(f.Shippers.Items);
        Assert.Equal(user.Id, shipper.UserId);
        Assert.Equal(1, f.Uow.SaveCount);
    }

    [Fact]
    public async Task Register_Restaurant_AlsoCreatesRestaurantOwnedByUser()
    {
        var f = new Fixture();

        await f.Service.RegisterAsync(Reg(UserRole.Restaurant, restaurantName: "  Pho Ha Noi "));

        var user = Assert.Single(f.Users.Items);
        var restaurant = Assert.Single(f.Restaurants.Items);
        Assert.Equal(user.Id, restaurant.OwnerUserId);
        Assert.Equal("Pho Ha Noi", restaurant.Name);
        Assert.Equal(1, f.Uow.SaveCount);
    }

    [Fact]
    public async Task Register_RestaurantWithoutName_ThrowsAndSavesNothing()
    {
        var f = new Fixture();

        await Assert.ThrowsAsync<DomainException>(() =>
            f.Service.RegisterAsync(Reg(UserRole.Restaurant, restaurantName: " ")));

        Assert.Empty(f.Users.Items);
        Assert.Equal(0, f.Uow.SaveCount);
    }

    [Fact]
    public async Task Register_Admin_IsForbidden()
    {
        var f = new Fixture();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            f.Service.RegisterAsync(Reg(UserRole.Admin)));

        Assert.Empty(f.Users.Items);
    }

    [Fact]
    public async Task Register_ShortPassword_Throws()
    {
        var f = new Fixture();

        await Assert.ThrowsAsync<DomainException>(() =>
            f.Service.RegisterAsync(Reg(UserRole.Customer, password: "1234567")));
    }

    [Fact]
    public async Task Register_DuplicateEmail_IgnoringCase_Conflicts()
    {
        var f = new Fixture();
        await f.Service.RegisterAsync(Reg(UserRole.Customer, "a@mail.com"));

        await Assert.ThrowsAsync<ConflictException>(() =>
            f.Service.RegisterAsync(Reg(UserRole.Customer, "A@MAIL.com")));

        Assert.Single(f.Users.Items);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsTokens()
    {
        var f = new Fixture();
        await f.Service.RegisterAsync(Reg(UserRole.Customer));

        var result = await f.Service.LoginAsync(new LoginRequest("a@mail.com", "password1"));

        Assert.StartsWith("access-", result.AccessToken);
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_GiveSameMessage()
    {
        var f = new Fixture();
        await f.Service.RegisterAsync(Reg(UserRole.Customer));

        var wrongPassword = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            f.Service.LoginAsync(new LoginRequest("a@mail.com", "wrong-password")));
        var unknownEmail = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            f.Service.LoginAsync(new LoginRequest("nobody@mail.com", "password1")));

        Assert.Equal(wrongPassword.Message, unknownEmail.Message);   // không để lộ email nào đã đăng ký
    }

    [Fact]
    public async Task Refresh_RotatesToken_OldOneIsRevoked()
    {
        var f = new Fixture();
        var first = await f.Service.RegisterAsync(Reg(UserRole.Customer));

        var second = await f.Service.RefreshAsync(first.RefreshToken);

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        var old = await f.Tokens.GetByHashAsync(f.TokenService.HashRefreshToken(first.RefreshToken));
        Assert.True(old!.IsRevoked);
    }

    [Fact]
    public async Task Refresh_ReusingRevokedToken_RevokesAllSessions()
    {
        var f = new Fixture();
        var first = await f.Service.RegisterAsync(Reg(UserRole.Customer));
        var second = await f.Service.RefreshAsync(first.RefreshToken);   // first bị thu hồi

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            f.Service.RefreshAsync(first.RefreshToken));                  // dùng lại token cũ

        // token mới (second) cũng bị thu hồi
        var current = await f.Tokens.GetByHashAsync(f.TokenService.HashRefreshToken(second.RefreshToken));
        Assert.True(current!.IsRevoked);
    }

    [Fact]
    public async Task Refresh_ExpiredToken_IsRejected()
    {
        var f = new Fixture();
        var user = new User("a@mail.com", "hashed:password1", "A", UserRole.Customer);
        await f.Users.AddAsync(user);
        await f.Tokens.AddAsync(new RefreshToken(user.Id, f.TokenService.HashRefreshToken("old"), DateTime.UtcNow.AddMinutes(-1)));

        await Assert.ThrowsAsync<UnauthorizedException>(() => f.Service.RefreshAsync("old"));
    }

    [Fact]
    public async Task Refresh_UnknownToken_IsRejected()
    {
        var f = new Fixture();

        await Assert.ThrowsAsync<UnauthorizedException>(() => f.Service.RefreshAsync("does-not-exist"));
    }

    [Fact]
    public async Task Logout_RevokesToken_AndCanBeCalledTwice()
    {
        var f = new Fixture();
        var result = await f.Service.RegisterAsync(Reg(UserRole.Customer));

        await f.Service.LogoutAsync(result.RefreshToken);
        await f.Service.LogoutAsync(result.RefreshToken);   // lần hai không lỗi

        await Assert.ThrowsAsync<UnauthorizedException>(() => f.Service.RefreshAsync(result.RefreshToken));
    }

    // ---------- Đồ giả (fake) thay cho cơ sở dữ liệu, BCrypt, JWT ----------
    private sealed class Fixture
    {
        public FakeUnitOfWork Uow { get; } = new();
        public FakeUserRepository Users { get; } = new();
        public FakeRefreshTokenRepository Tokens { get; } = new();
        public FakeShipperRepository Shippers { get; } = new();
        public FakeRestaurantRepository Restaurants { get; } = new();
        public FakeTokenService TokenService { get; } = new();
        public AuthService Service { get; }

        public Fixture() =>
            Service = new AuthService(Users, Tokens, Shippers, Restaurants, new FakeHasher(), TokenService, Uow);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken ct = default) { SaveCount++; return Task.FromResult(1); }
        Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken, bool _) { SaveCount++; return Task.CompletedTask; }
    }

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => "hashed:" + password;
        public bool Verify(string password, string hash) => hash == "hashed:" + password;
    }

    private sealed class FakeTokenService : ITokenService
    {
        public string CreateAccessToken(User user) => "access-" + user.Id;
        public (string Plain, RefreshToken Entity) CreateRefreshToken(Guid userId)
        {
            var plain = Guid.NewGuid().ToString("N");
            return (plain, new RefreshToken(userId, HashRefreshToken(plain), DateTime.UtcNow.AddDays(7)));
        }
        public string HashRefreshToken(string plain) => "h:" + plain;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Items { get; } = new();
        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Email == email));
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(Items.Any(x => x.Email == email));
        public Task AddAsync(User user, CancellationToken ct = default) { Items.Add(user); return Task.CompletedTask; }
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<RefreshToken> Items { get; } = new();
        public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.TokenHash == tokenHash));
        public Task AddAsync(RefreshToken token, CancellationToken ct = default) { Items.Add(token); return Task.CompletedTask; }
        public Task RevokeAllActiveAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default)
        {
            foreach (var t in Items.Where(x => x.UserId == userId && !x.IsRevoked)) t.Revoke(nowUtc);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeShipperRepository : IShipperRepository
    {
        public List<Shipper> Items { get; } = new();
        public Task<Shipper?> GetByIdAsync(Guid shipperId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == shipperId));
        public Task<IReadOnlyList<Shipper>> GetAvailableShippersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Shipper>>(Items.Where(x => x.IsAvailable).ToList());
        public Task SaveWithConcurrencyAsync(Shipper shipper, int expectedVersion, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task<Shipper?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.UserId == userId));
        public Task AddAsync(Shipper shipper, CancellationToken ct = default) { Items.Add(shipper); return Task.CompletedTask; }
    }

    private sealed class FakeRestaurantRepository : IRestaurantRepository
    {
        public List<Restaurant> Items { get; } = new();
        public Task<Restaurant?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Restaurant>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Restaurant>>(Items.ToList());
        public Task<Restaurant?> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.OwnerUserId == ownerUserId));
        public Task AddAsync(Restaurant restaurant, CancellationToken ct = default) { Items.Add(restaurant); return Task.CompletedTask; }
    }
}
