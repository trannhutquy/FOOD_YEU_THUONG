using FoodHub.Application.Restaurants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Api.Controllers;

[ApiController]
[Route("api/restaurants")]
public class RestaurantController(IRestaurantService restaurants) : ControllerBase
{
    /// <summary>Lấy danh sách tất cả quán ăn kèm trạng thái mở cửa.</summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RestaurantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await restaurants.GetAllAsync(ct));

    /// <summary>Xem thông tin chi tiết một quán ăn.</summary>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RestaurantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await restaurants.GetByIdAsync(id, ct));

    /// <summary>Tạo quán ăn mới thuộc quyền sở hữu của chủ quán hiện tại.</summary>
    [Authorize(Roles = "Restaurant")]
    [HttpPost]
    [ProducesResponseType(typeof(RestaurantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(CreateRestaurantRequest request, CancellationToken ct)
    {
        var created = await restaurants.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cập nhật tên và trạng thái mở cửa của quán (chỉ chủ sở hữu).</summary>
    [Authorize(Roles = "Restaurant")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RestaurantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateRestaurantRequest request, CancellationToken ct) =>
        Ok(await restaurants.UpdateAsync(id, request, ct));

    /// <summary>Mở hoặc đóng cửa quán ăn (chỉ chủ sở hữu).</summary>
    [Authorize(Roles = "Restaurant")]
    [HttpPatch("{id:guid}/open")]
    [ProducesResponseType(typeof(RestaurantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetOpen(Guid id, SetRestaurantOpenRequest request, CancellationToken ct) =>
        Ok(await restaurants.SetOpenAsync(id, request, ct));
}
