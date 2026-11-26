namespace FoodHub.Application.Restaurants;

public interface IRestaurantService
{
    Task<IReadOnlyList<RestaurantDto>> GetAllAsync(CancellationToken ct = default);
    Task<RestaurantDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RestaurantDto> CreateAsync(CreateRestaurantRequest request, CancellationToken ct = default);
    Task<RestaurantDto> UpdateAsync(Guid restaurantId, UpdateRestaurantRequest request, CancellationToken ct = default);
    Task<RestaurantDto> SetOpenAsync(Guid restaurantId, SetRestaurantOpenRequest request, CancellationToken ct = default);
}

public interface IMenuItemService
{
    Task<IReadOnlyList<MenuItemDto>> GetPublicMenuAsync(Guid restaurantId, CancellationToken ct = default);
    Task<IReadOnlyList<MenuItemDto>> GetOwnerMenuAsync(Guid restaurantId, CancellationToken ct = default);
    Task<MenuItemDto> AddAsync(Guid restaurantId, CreateMenuItemRequest request, CancellationToken ct = default);
    Task<MenuItemDto> UpdateAsync(Guid restaurantId, Guid itemId, UpdateMenuItemRequest request, CancellationToken ct = default);
    Task<MenuItemDto> SetAvailabilityAsync(Guid restaurantId, Guid itemId, SetMenuItemAvailabilityRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid restaurantId, Guid itemId, CancellationToken ct = default);
}
