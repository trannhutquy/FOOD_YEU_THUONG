using FoodHub.Domain;

namespace FoodHub.Application.Vouchers;

public interface IVoucherService
{
    // Gọi lúc tạo đơn; trả về số tiền được giảm (không gọi SaveChanges để dùng chung transaction với Order).
    Task<Money> RedeemAsync(string code, Guid customerId, Money subtotal, Money shippingFee, CancellationToken ct = default);
    Task RefundAsync(string code, Guid customerId, CancellationToken ct = default);

    Task<VoucherDto> CreateAsync(CreateVoucherRequest request, CancellationToken ct = default);
    Task<VoucherValidationDto> ValidateAsync(string code, decimal subtotal, decimal shippingFee = 0, CancellationToken ct = default);
    Task<IReadOnlyList<VoucherDto>> GetAllAsync(CancellationToken ct = default);
}
