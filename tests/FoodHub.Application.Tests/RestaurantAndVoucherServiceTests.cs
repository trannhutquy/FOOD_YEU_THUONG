using FoodHub.Application.Abstractions;
using FoodHub.Application.Restaurants;
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

    private sealed class Fixture
    {
        public FakeUnitOfWork Uow { get; } = new();
        public FakeRestaurantRepository Restaurants { get; } = new();
        public FakeMenuItemRepository MenuItems { get; } = new();
        public RestaurantService RestaurantService { get; }
        public MenuItemService MenuItemService { get; }

        public Fixture(Guid userId, UserRole role)
        {
            var currentUser = new FakeCurrentUser(userId, role);
            RestaurantService = new RestaurantService(Restaurants, currentUser, Uow);
            MenuItemService = new MenuItemService(Restaurants, MenuItems, currentUser, Uow);
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
        Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken, bool _) => Task.CompletedTask;
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
        public Task<IReadOnlyList<MenuItem>> GetByIdsAsync(IEnumerable<Guid> menuItemIds, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MenuItem>>(Items.Where(x => menuItemIds.Contains(x.Id)).ToList());
        public Task AddAsync(MenuItem item, CancellationToken ct = default) { Items.Add(item); return Task.CompletedTask; }
        public void Remove(MenuItem item) => Items.Remove(item);
    }
}
