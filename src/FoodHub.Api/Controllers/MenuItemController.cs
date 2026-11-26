using FoodHub.Application.Restaurants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Api.Controllers;

[ApiController]
[Route("api/restaurants/{restaurantId:guid}/menu")]
public class MenuItemController(IMenuItemService menuItems) : ControllerBase
{
    /// <summary>Lấy thực đơn công khai của quán (chỉ gồm các món đang còn bán).</summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MenuItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicMenu(Guid restaurantId, CancellationToken ct) =>
        Ok(await menuItems.GetPublicMenuAsync(restaurantId, ct));

    /// <summary>Lấy toàn bộ thực đơn của quán để chủ quán quản lý (bao gồm món tạm hết).</summary>
    [Authorize(Roles = "Restaurant")]
    [HttpGet("all")]
    [ProducesResponseType(typeof(IReadOnlyList<MenuItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOwnerMenu(Guid restaurantId, CancellationToken ct) =>
        Ok(await menuItems.GetOwnerMenuAsync(restaurantId, ct));

    /// <summary>Thêm món mới vào thực đơn của quán (chỉ chủ quán).</summary>
    [Authorize(Roles = "Restaurant")]
    [HttpPost]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(Guid restaurantId, CreateMenuItemRequest request, CancellationToken ct)
    {
        var created = await menuItems.AddAsync(restaurantId, request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Cập nhật thông tin món ăn (chỉ chủ quán).</summary>
    [Authorize(Roles = "Restaurant")]
    [HttpPut("{itemId:guid}")]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid restaurantId, Guid itemId, UpdateMenuItemRequest request, CancellationToken ct) =>
        Ok(await menuItems.UpdateAsync(restaurantId, itemId, request, ct));

    /// <summary>Bật hoặc tắt trạng thái còn bán của món ăn (chỉ chủ quán).</summary>
    [Authorize(Roles = "Restaurant")]
    [HttpPatch("{itemId:guid}/availability")]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetAvailability(Guid restaurantId, Guid itemId, SetMenuItemAvailabilityRequest request, CancellationToken ct) =>
        Ok(await menuItems.SetAvailabilityAsync(restaurantId, itemId, request, ct));

    /// <summary>Xoá món ăn khỏi thực đơn (chỉ chủ quán).</summary>
    [Authorize(Roles = "Restaurant")]
    [HttpDelete("{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid restaurantId, Guid itemId, CancellationToken ct)
    {
        await menuItems.DeleteAsync(restaurantId, itemId, ct);
        return NoContent();
    }
}
