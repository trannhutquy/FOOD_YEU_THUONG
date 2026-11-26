using System.ComponentModel.DataAnnotations;
using FoodHub.Domain;

namespace FoodHub.Application.Restaurants;

public record CreateRestaurantRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    bool IsOpen = true);

public record UpdateRestaurantRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    bool? IsOpen = null);

public record SetRestaurantOpenRequest(bool IsOpen);

public record RestaurantDto(Guid Id, string Name, Guid OwnerUserId, bool IsOpen)
{
    public static RestaurantDto From(Restaurant restaurant) =>
        new(restaurant.Id, restaurant.Name, restaurant.OwnerUserId, restaurant.IsOpen);
}

public record CreateMenuItemRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")] decimal Price,
    bool IsAvailable = true);

public record UpdateMenuItemRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")] decimal Price,
    bool IsAvailable = true);

public record SetMenuItemAvailabilityRequest(bool IsAvailable);

public record MenuItemDto(Guid Id, Guid RestaurantId, string Name, decimal Price, bool IsAvailable)
{
    public static MenuItemDto From(MenuItem item) =>
        new(item.Id, item.RestaurantId, item.Name, item.Price.Amount, item.IsAvailable);
}
