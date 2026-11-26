using FoodHub.Application.Abstractions;
using FoodHub.Application.Restaurants;
using FoodHub.Application.Vouchers;
using FoodHub.Domain;
using Xunit;

namespace FoodHub.Application.Tests;

public class RestaurantAndVoucherServiceTests
{
    [Fact]
    public async Task RestaurantService_NonOwnerCannotUpdateOrToggleOpen()
    {
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var f = new Fixture(otherOwnerId, UserRole.Restaurant);
        var restaurant = new Restaurant("Com Tam", ownerId);
        await f.Restaurants.AddAsync(restaurant);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            f.RestaurantService.UpdateAsync(restaurant.Id, new UpdateRestaurantRequest("Hacked")));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            f.RestaurantService.SetOpenAsync(restaurant.Id, new SetRestaurantOpenRequest(false)));
    }

    [Fact]
    public async Task MenuItemService_PublicMenu_OnlyReturnsAvailableItems()
    {
        var ownerId = Guid.NewGuid();
        var f = new Fixture(ownerId, UserRole.Restaurant);
        var restaurant = new Restaurant("Pho 24", ownerId);
        await f.Restaurants.AddAsync(restaurant);
        await f.MenuItems.AddAsync(new MenuItem(restaurant.Id, "Pho Tai", new Money(50000), isAvailable: true));
        await f.MenuItems.AddAsync(new MenuItem(restaurant.Id, "Pho Bo Vien", new Money(50000), isAvailable: false));

        var publicMenu = await f.MenuItemService.GetPublicMenuAsync(restaurant.Id);
        var ownerMenu = await f.MenuItemService.GetOwnerMenuAsync(restaurant.Id);

        Assert.Single(publicMenu);
        Assert.Equal("Pho Tai", publicMenu[0].Name);
        Assert.Equal(2, ownerMenu.Count);
    }

    [Fact]
    public async Task MenuItemService_NonOwnerCannotModifyMenu()
    {
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var f = new Fixture(otherOwnerId, UserRole.Restaurant);
        var restaurant = new Restaurant("Pho 24", ownerId);
        var item = new MenuItem(restaurant.Id, "Pho Tai", new Money(50000));
        await f.Restaurants.AddAsync(restaurant);
        await f.MenuItems.AddAsync(item);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            f.MenuItemService.AddAsync(restaurant.Id, new CreateMenuItemRequest("New", 10000)));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            f.MenuItemService.UpdateAsync(restaurant.Id, item.Id, new UpdateMenuItemRequest("New", 20000)));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            f.MenuItemService.DeleteAsync(restaurant.Id, item.Id));
    }

    [Fact]
    public async Task VoucherService_CreateDuplicateCode_ThrowsConflict()
    {
        var f = new Fixture(Guid.NewGuid(), UserRole.Admin);
        var req = new CreateVoucherRequest("sale10", VoucherType.Percentage, 10, 20000, 50000, DateTime.UtcNow.AddDays(5), 50);

        var created = await f.VoucherService.CreateAsync(req);
        Assert.Equal("SALE10", created.Code);

        await Assert.ThrowsAsync<ConflictException>(() =>
            f.VoucherService.CreateAsync(req with { Code = " SALE10 " }));
    }

    [Fact]
    public async Task VoucherService_ValidateRedeemAndRefund_WorkTogether()
    {
        var customerId = Guid.NewGuid();
        var f = new Fixture(customerId, UserRole.Customer);
        var voucher = new Voucher("SAVE15K", VoucherType.FixedAmount, 15000, null, 50000, DateTime.UtcNow.AddDays(5), 5);
        await f.Vouchers.AddAsync(voucher);

        var preview = await f.VoucherService.ValidateAsync("save15k", 100000, 15000);
        Assert.Equal(15000m, preview.DiscountAmount);
        Assert.Equal(100000m, preview.FinalTotal);
        Assert.Equal(0, voucher.UsedCount);

        var discount = await f.VoucherService.RedeemAsync("SAVE15K", customerId, new Money(100000), new Money(15000));
        Assert.Equal(15000m, discount.Amount);
        Assert.Equal(1, voucher.UsedCount);

        await f.VoucherService.RefundAsync("SAVE15K", customerId);
        Assert.Equal(0, voucher.UsedCount);
    }

    private sealed class Fixture
    {
        public FakeUnitOfWork Uow { get; } = new();
        public FakeRestaurantRepository Restaurants { get; } = new();
        public FakeMenuItemRepository MenuItems { get; } = new();
        public FakeVoucherRepository Vouchers { get; } = new();
        public RestaurantService RestaurantService { get; }
        public MenuItemService MenuItemService { get; }
        public VoucherService VoucherService { get; }

        public Fixture(Guid userId, UserRole role)
        {
            var currentUser = new FakeCurrentUser(userId, role);
            RestaurantService = new RestaurantService(Restaurants, currentUser, Uow);
            MenuItemService = new MenuItemService(Restaurants, MenuItems, currentUser, Uow);
            VoucherService = new VoucherService(Vouchers, currentUser, Uow);
        }
    }

    private sealed class FakeCurrentUser(Guid id, UserRole role) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid Id => id;
        public UserRole Role => role;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
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

    private sealed class FakeMenuItemRepository : IMenuItemRepository
    {
        public List<MenuItem> Items { get; } = new();
        public Task<MenuItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<MenuItem>> GetByRestaurantIdAsync(Guid restaurantId, bool availableOnly, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MenuItem>>(Items.Where(x => x.RestaurantId == restaurantId && (!availableOnly || x.IsAvailable)).ToList());
        public Task AddAsync(MenuItem item, CancellationToken ct = default) { Items.Add(item); return Task.CompletedTask; }
        public void Remove(MenuItem item) => Items.Remove(item);
    }

    private sealed class FakeVoucherRepository : IVoucherRepository
    {
        public List<Voucher> Items { get; } = new();
        public Task<Voucher?> GetByCodeAsync(string code, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Code == code.Trim().ToUpperInvariant()));
        public Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default) =>
            Task.FromResult(Items.Any(x => x.Code == code.Trim().ToUpperInvariant()));
        public Task<IReadOnlyList<Voucher>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Voucher>>(Items.ToList());
        public Task AddAsync(Voucher voucher, CancellationToken ct = default) { Items.Add(voucher); return Task.CompletedTask; }
    }
}
