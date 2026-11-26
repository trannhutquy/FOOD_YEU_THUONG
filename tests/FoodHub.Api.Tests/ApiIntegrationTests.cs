using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodHub.Application.Abstractions;
using FoodHub.Application.Auth;
using FoodHub.Application.Restaurants;
using FoodHub.Application.Vouchers;
using FoodHub.Domain;
using FoodHub.Infrastructure.Auth;
using FoodHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace FoodHub.Api.Tests;

public class ApiIntegrationTests
{
    private const string TestJwtKey = "test-super-secret-jwt-key-at-least-32-chars!";
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static HttpClient CreateClient(out FoodHubWebApplicationFactory factory)
    {
        factory = new FoodHubWebApplicationFactory();
        return factory.CreateClient();
    }

    private static string CreateJwt(Guid userId, UserRole role, DateTime expiresUtc)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, $"{role.ToString().ToLowerInvariant()}@mail.com"),
            new Claim("role", role.ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var token = new JwtSecurityToken(
            issuer: "foodhub",
            audience: "foodhub-clients",
            claims: claims,
            expires: expiresUtc,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task Auth_RegisterDuplicateEmail_Returns409_AndRegisterAdmin_Returns403()
    {
        using var client = CreateClient(out var factory);
        using var _ = factory;

        var first = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("vy@mail.com", "matkhau123", "Thuy Vy", UserRole.Customer, null));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var duplicate = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("VY@MAIL.COM", "matkhau123", "Thuy Vy", UserRole.Customer, null));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var admin = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("admin@mail.com", "matkhau123", "Admin", UserRole.Admin, null));
        Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode);
    }

    [Fact]
    public async Task Auth_LoginFailureMessagesMatch_AndExpiredTokenIsRejected()
    {
        using var client = CreateClient(out var factory);
        using var _ = factory;

        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("khach@mail.com", "matkhau123", "Khach", UserRole.Customer, null));

        var wrongPw = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("khach@mail.com", "sai-mat-khau"));
        var unknownEmail = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("khongtontai@mail.com", "matkhau123"));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPw.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(await wrongPw.Content.ReadAsStringAsync(), await unknownEmail.Content.ReadAsStringAsync());

        // No token -> 401
        var noToken = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);

        // Expired token -> 401
        var expiredToken = CreateJwt(Guid.NewGuid(), UserRole.Customer, DateTime.UtcNow.AddMinutes(-10));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);
        var expiredRes = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, expiredRes.StatusCode);
    }

    [Fact]
    public async Task Auth_RefreshTokenReuse_RevokesEntireSession()
    {
        using var client = CreateClient(out var factory);
        using var _ = factory;

        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("rotate@mail.com", "matkhau123", "User", UserRole.Customer, null));
        var tokens1 = await reg.Content.ReadFromJsonAsync<AuthResult>();

        var refresh1 = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(tokens1!.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refresh1.StatusCode);
        var tokens2 = await refresh1.Content.ReadFromJsonAsync<AuthResult>();

        // Reuse old token -> 401 and revokes tokens2
        var reuseOld = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(tokens1.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseOld.StatusCode);

        var useNewAfterReuse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(tokens2!.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, useNewAfterReuse.StatusCode);
    }

    [Fact]
    public async Task RestaurantAndMenu_RoleAndOwnershipAuthorization_AreEnforced()
    {
        using var client = CreateClient(out var factory);
        using var _ = factory;

        // Register Owner A, Owner B, and Customer
        var regA = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("ownera@mail.com", "matkhau123", "Owner A", UserRole.Restaurant, "Quan A"));
        var tokenA = (await regA.Content.ReadFromJsonAsync<AuthResult>())!.AccessToken;

        var regB = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("ownerb@mail.com", "matkhau123", "Owner B", UserRole.Restaurant, "Quan B"));
        var tokenB = (await regB.Content.ReadFromJsonAsync<AuthResult>())!.AccessToken;

        var regC = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("cust@mail.com", "matkhau123", "Customer", UserRole.Customer, null));
        var tokenCustomer = (await regC.Content.ReadFromJsonAsync<AuthResult>())!.AccessToken;

        var restaurants = await client.GetFromJsonAsync<List<RestaurantDto>>("/api/restaurants");
        Assert.NotNull(restaurants);
        Assert.Equal(2, restaurants.Count);
        var restaurantA = restaurants.Single(r => r.Name == "Quan A");

        // Customer cannot add menu item -> 403
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenCustomer);
        var custAdd = await client.PostAsJsonAsync($"/api/restaurants/{restaurantA.Id}/menu",
            new CreateMenuItemRequest("Pho Bo", 45000));
        Assert.Equal(HttpStatusCode.Forbidden, custAdd.StatusCode);

        // Owner B cannot add menu item to Owner A's restaurant -> 403
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var ownerBAdd = await client.PostAsJsonAsync($"/api/restaurants/{restaurantA.Id}/menu",
            new CreateMenuItemRequest("Pho Bo", 45000));
        Assert.Equal(HttpStatusCode.Forbidden, ownerBAdd.StatusCode);

        // Owner A adds 2 items (1 available, 1 unavailable) -> 201
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var add1 = await client.PostAsJsonAsync($"/api/restaurants/{restaurantA.Id}/menu",
            new CreateMenuItemRequest("Pho Tai", 50000, IsAvailable: true));
        Assert.Equal(HttpStatusCode.Created, add1.StatusCode);
        var item1 = await add1.Content.ReadFromJsonAsync<MenuItemDto>();

        var add2 = await client.PostAsJsonAsync($"/api/restaurants/{restaurantA.Id}/menu",
            new CreateMenuItemRequest("Pho Dac Biet", 65000, IsAvailable: false));
        Assert.Equal(HttpStatusCode.Created, add2.StatusCode);

        // Public menu returns ONLY available items
        client.DefaultRequestHeaders.Authorization = null;
        var publicMenu = await client.GetFromJsonAsync<List<MenuItemDto>>($"/api/restaurants/{restaurantA.Id}/menu");
        Assert.Single(publicMenu!);
        Assert.Equal("Pho Tai", publicMenu![0].Name);

        // Owner A updates item1 and closes restaurant
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var putRes = await client.PutAsJsonAsync($"/api/restaurants/{restaurantA.Id}/menu/{item1!.Id}",
            new UpdateMenuItemRequest("Pho Tai Nam", 55000, IsAvailable: true));
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);

        var closeRes = await client.PatchAsJsonAsync($"/api/restaurants/{restaurantA.Id}/open",
            new SetRestaurantOpenRequest(false));
        Assert.Equal(HttpStatusCode.OK, closeRes.StatusCode);
        var updatedRest = await closeRes.Content.ReadFromJsonAsync<RestaurantDto>();
        Assert.False(updatedRest!.IsOpen);

        // Owner A gets single restaurant, updates name, toggles item availability, views owner menu, and deletes item
        var singleRest = await client.GetFromJsonAsync<RestaurantDto>($"/api/restaurants/{restaurantA.Id}");
        Assert.Equal("Quan A", singleRest!.Name);

        var renameRes = await client.PutAsJsonAsync($"/api/restaurants/{restaurantA.Id}",
            new UpdateRestaurantRequest("Quan A Moi", true));
        Assert.Equal(HttpStatusCode.OK, renameRes.StatusCode);

        var ownerCreateRest = await client.PostAsJsonAsync("/api/restaurants",
            new CreateRestaurantRequest("Chi nhanh 2", true));
        Assert.Equal(HttpStatusCode.Created, ownerCreateRest.StatusCode);

        var ownerMenu = await client.GetFromJsonAsync<List<MenuItemDto>>($"/api/restaurants/{restaurantA.Id}/menu/all");
        Assert.Equal(2, ownerMenu!.Count);

        var toggleRes = await client.PatchAsJsonAsync($"/api/restaurants/{restaurantA.Id}/menu/{item1.Id}/availability",
            new SetMenuItemAvailabilityRequest(false));
        Assert.Equal(HttpStatusCode.OK, toggleRes.StatusCode);

        var deleteRes = await client.DeleteAsync($"/api/restaurants/{restaurantA.Id}/menu/{item1.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        var missingRest = await client.GetAsync($"/api/restaurants/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missingRest.StatusCode);
    }

    [Fact]
    public async Task Voucher_AdminCreatesAndCustomerValidates_RoleChecksAndSwaggerWork()
    {
        using var client = CreateClient(out var factory);
        using var _ = factory;

        var regCust = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("voucher-cust@mail.com", "matkhau123", "Khach", UserRole.Customer, null));
        var tokenCustomer = (await regCust.Content.ReadFromJsonAsync<AuthResult>())!.AccessToken;

        var adminToken = CreateJwt(Guid.NewGuid(), UserRole.Admin, DateTime.UtcNow.AddMinutes(15));

        var createReq = new CreateVoucherRequest(
            "FOOD20", VoucherType.Percentage, 20, 30000, 50000, DateTime.UtcNow.AddDays(7), 10);

        // Customer cannot create voucher -> 403
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenCustomer);
        var custCreate = await client.PostAsJsonAsync("/api/vouchers", createReq);
        Assert.Equal(HttpStatusCode.Forbidden, custCreate.StatusCode);

        // Admin creates voucher -> 201, duplicate -> 409, and Admin lists vouchers -> 200
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var adminCreate = await client.PostAsJsonAsync("/api/vouchers", createReq);
        Assert.Equal(HttpStatusCode.Created, adminCreate.StatusCode);

        var duplicateCreate = await client.PostAsJsonAsync("/api/vouchers", createReq);
        Assert.Equal(HttpStatusCode.Conflict, duplicateCreate.StatusCode);

        var listRes = await client.GetFromJsonAsync<List<VoucherDto>>("/api/vouchers", JsonOpts);
        Assert.Single(listRes!);

        // Customer validates voucher -> 200 (20% of 200,000 = 40,000, capped at 30,000)
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenCustomer);
        var validateRes = await client.GetAsync("/api/vouchers/validate?code=food20&subtotal=200000&shippingFee=15000");
        Assert.Equal(HttpStatusCode.OK, validateRes.StatusCode);
        var validation = await validateRes.Content.ReadFromJsonAsync<VoucherValidationDto>(JsonOpts);
        Assert.Equal(30000m, validation!.DiscountAmount);
        Assert.Equal(185000m, validation.FinalTotal);

        // Swagger document is generated cleanly
        client.DefaultRequestHeaders.Authorization = null;
        var swaggerRes = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, swaggerRes.StatusCode);
    }

    [Fact]
    public async Task Auth_LogoutAndRegisterValidation_WorkAsExpected()
    {
        using var client = CreateClient(out var factory);
        using var _ = factory;

        // Restaurant without restaurantName -> 400
        var missingName = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("no-name@mail.com", "matkhau123", "Owner", UserRole.Restaurant, " "));
        Assert.Equal(HttpStatusCode.BadRequest, missingName.StatusCode);

        // Register shipper -> 201 and /me returns Shipper role
        var shipperReg = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("shipper@mail.com", "matkhau123", "Shipper A", UserRole.Shipper, null));
        Assert.Equal(HttpStatusCode.Created, shipperReg.StatusCode);
        var tokens = await shipperReg.Content.ReadFromJsonAsync<AuthResult>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var meRes = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);
        var me = await meRes.Content.ReadFromJsonAsync<MeDto>(JsonOpts);
        Assert.Equal(UserRole.Shipper, me!.Role);

        // Logout revokes refresh token
        var logoutRes = await client.PostAsJsonAsync("/api/auth/logout", new RefreshRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logoutRes.StatusCode);

        var refreshAfterLogout = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }

    private sealed class FoodHubWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = "FoodHubTestDb_" + Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Jwt:Key", TestJwtKey);
            builder.UseSetting("Jwt:Issuer", "foodhub");
            builder.UseSetting("Jwt:Audience", "foodhub-clients");
            builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Database=unused");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = TestJwtKey,
                    ["Jwt:Issuer"] = "foodhub",
                    ["Jwt:Audience"] = "foodhub-clients",
                    ["ConnectionStrings:Default"] = "Host=localhost;Database=unused"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<IUnitOfWork>();

                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
                services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
            });
        }
    }
}
