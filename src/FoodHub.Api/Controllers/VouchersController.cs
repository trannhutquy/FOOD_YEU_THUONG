using FoodHub.Application.Vouchers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodHub.Api.Controllers;

[ApiController]
[Route("api/vouchers")]
public class VouchersController(IVoucherService vouchers) : ControllerBase
{
    /// <summary>Tạo mã khuyến mãi mới (chỉ Admin).</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(VoucherDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateVoucherRequest request, CancellationToken ct)
    {
        var created = await vouchers.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Lấy danh sách các mã khuyến mãi (chỉ Admin).</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VoucherDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await vouchers.GetAllAsync(ct));

    /// <summary>Kiểm tra và xem trước số tiền giảm của mã khuyến mãi (chỉ Customer, không trừ lượt).</summary>
    [Authorize(Roles = "Customer")]
    [HttpGet("validate")]
    [ProducesResponseType(typeof(VoucherValidationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Validate(
        [FromQuery] string code,
        [FromQuery] decimal subtotal,
        [FromQuery] decimal shippingFee = 0,
        CancellationToken ct = default) =>
        Ok(await vouchers.ValidateAsync(code, subtotal, shippingFee, ct));
}
