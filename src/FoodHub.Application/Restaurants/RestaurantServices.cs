using FoodHub.Application.Abstractions;
using FoodHub.Domain;

namespace FoodHub.Application.Restaurants;

public class RestaurantService(
    IRestaurantRepository restaurants,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRestaurantService
{
    public async Task<IReadOnlyList<RestaurantDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await restaurants.GetAllAsync(ct);
        return items.Select(RestaurantDto.From).ToList();
    }

    public async Task<RestaurantDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var restaurant = await restaurants.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Restaurant not found.");
        return RestaurantDto.From(restaurant);
    }

    public async Task<RestaurantDto> CreateAsync(CreateRestaurantRequest request, CancellationToken ct = default)
    {
        var restaurant = new Restaurant(request.Name, currentUser.Id, request.IsOpen);
        await restaurants.AddAsync(restaurant, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return RestaurantDto.From(restaurant);
    }

    public async Task<RestaurantDto> UpdateAsync(Guid restaurantId, UpdateRestaurantRequest request, CancellationToken ct = default)
    {
        var restaurant = await GetOwnedRestaurantAsync(restaurantId, ct);
        restaurant.Rename(request.Name);
        if (request.IsOpen is not null)
            restaurant.SetOpen(request.IsOpen.Value);

        await unitOfWork.SaveChangesAsync(ct);
        return RestaurantDto.From(restaurant);
    }

    public async Task<RestaurantDto> SetOpenAsync(Guid restaurantId, SetRestaurantOpenRequest request, CancellationToken ct = default)
    {
        var restaurant = await GetOwnedRestaurantAsync(restaurantId, ct);
        restaurant.SetOpen(request.IsOpen);
        await unitOfWork.SaveChangesAsync(ct);
        return RestaurantDto.From(restaurant);
    }

    private async Task<Restaurant> GetOwnedRestaurantAsync(Guid restaurantId, CancellationToken ct)
    {
        var restaurant = await restaurants.GetByIdAsync(restaurantId, ct)
            ?? throw new NotFoundException("Restaurant not found.");
        if (restaurant.OwnerUserId != currentUser.Id)
            throw new ForbiddenException("You do not own this restaurant.");
        return restaurant;
    }
}

public class MenuItemService(
    IRestaurantRepository restaurants,
    IMenuItemRepository menuItems,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IMenuItemService
{
    public async Task<IReadOnlyList<MenuItemDto>> GetPublicMenuAsync(Guid restaurantId, CancellationToken ct = default)
    {
        _ = await restaurants.GetByIdAsync(restaurantId, ct)
            ?? throw new NotFoundException("Restaurant not found.");

        var items = await menuItems.GetByRestaurantIdAsync(restaurantId, availableOnly: true, ct);
        return items.Select(MenuItemDto.From).ToList();
    }

    public async Task<IReadOnlyList<MenuItemDto>> GetOwnerMenuAsync(Guid restaurantId, CancellationToken ct = default)
    {
        await EnsureOwnedRestaurantAsync(restaurantId, ct);
        var items = await menuItems.GetByRestaurantIdAsync(restaurantId, availableOnly: false, ct);
        return items.Select(MenuItemDto.From).ToList();
    }

    public async Task<MenuItemDto> AddAsync(Guid restaurantId, CreateMenuItemRequest request, CancellationToken ct = default)
    {
        var restaurant = await EnsureOwnedRestaurantAsync(restaurantId, ct);
        var item = new MenuItem(restaurant.Id, request.Name, new Money(request.Price), request.IsAvailable);
        await menuItems.AddAsync(item, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return MenuItemDto.From(item);
    }

    public async Task<MenuItemDto> UpdateAsync(Guid restaurantId, Guid itemId, UpdateMenuItemRequest request, CancellationToken ct = default)
    {
        await EnsureOwnedRestaurantAsync(restaurantId, ct);
        var item = await GetOwnedItemAsync(restaurantId, itemId, ct);

        item.Rename(request.Name);
        item.ChangePrice(new Money(request.Price));
        item.SetAvailability(request.IsAvailable);

        await unitOfWork.SaveChangesAsync(ct);
        return MenuItemDto.From(item);
    }

    public async Task<MenuItemDto> SetAvailabilityAsync(Guid restaurantId, Guid itemId, SetMenuItemAvailabilityRequest request, CancellationToken ct = default)
    {
        await EnsureOwnedRestaurantAsync(restaurantId, ct);
        var item = await GetOwnedItemAsync(restaurantId, itemId, ct);

        item.SetAvailability(request.IsAvailable);
        await unitOfWork.SaveChangesAsync(ct);
        return MenuItemDto.From(item);
    }

    public async Task DeleteAsync(Guid restaurantId, Guid itemId, CancellationToken ct = default)
    {
        await EnsureOwnedRestaurantAsync(restaurantId, ct);
        var item = await GetOwnedItemAsync(restaurantId, itemId, ct);

        menuItems.Remove(item);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Restaurant> EnsureOwnedRestaurantAsync(Guid restaurantId, CancellationToken ct)
    {
        var restaurant = await restaurants.GetByIdAsync(restaurantId, ct)
            ?? throw new NotFoundException("Restaurant not found.");
        if (restaurant.OwnerUserId != currentUser.Id)
            throw new ForbiddenException("You do not own this restaurant.");
        return restaurant;
    }

    private async Task<MenuItem> GetOwnedItemAsync(Guid restaurantId, Guid itemId, CancellationToken ct)
    {
        var item = await menuItems.GetByIdAsync(itemId, ct)
            ?? throw new NotFoundException("Menu item not found.");
        if (item.RestaurantId != restaurantId)
            throw new NotFoundException("Menu item not found in this restaurant.");
        return item;
    }
}
