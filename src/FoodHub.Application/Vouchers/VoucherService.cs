using FoodHub.Application.Abstractions;
using FoodHub.Domain;

namespace FoodHub.Application.Vouchers;

public class VoucherService(
    IVoucherRepository vouchers,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IVoucherService
{
    public async Task<VoucherDto> CreateAsync(CreateVoucherRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new DomainException("Voucher code is required.");

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        if (await vouchers.ExistsByCodeAsync(normalizedCode, ct))
            throw new ConflictException("Voucher code already exists.");

        var voucher = new Voucher(
            normalizedCode,
            request.Type,
            request.Value,
            request.MaxDiscount,
            request.MinOrderValue,
            request.ExpiresAtUtc,
            request.UsageLimit);

        await vouchers.AddAsync(voucher, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return VoucherDto.From(voucher);
    }

    public async Task<IReadOnlyList<VoucherDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await vouchers.GetAllAsync(ct);
        return items.Select(VoucherDto.From).ToList();
    }

    public async Task<VoucherValidationDto> ValidateAsync(
        string code,
        decimal subtotal,
        decimal shippingFee = 0,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Voucher code is required.");

        var normalizedCode = code.Trim().ToUpperInvariant();
        var voucher = await vouchers.GetByCodeAsync(normalizedCode, ct)
            ?? throw new NotFoundException("Voucher not found.");

        var subtotalMoney = new Money(subtotal);
        var shippingMoney = new Money(shippingFee);
        var discount = voucher.Preview(currentUser.Id, subtotalMoney, shippingMoney, DateTime.UtcNow);
        var rawTotal = subtotalMoney + shippingMoney;
        var appliedDiscount = Money.Min(discount, rawTotal);

        return new VoucherValidationDto(
            voucher.Code,
            voucher.Type,
            appliedDiscount.Amount,
            (rawTotal - appliedDiscount).Amount);
    }

    public async Task<Money> RedeemAsync(
        string code,
        Guid customerId,
        Money subtotal,
        Money shippingFee,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Voucher code is required.");

        var voucher = await vouchers.GetByCodeAsync(code.Trim().ToUpperInvariant(), ct)
            ?? throw new NotFoundException("Voucher not found.");

        return voucher.Redeem(customerId, subtotal, shippingFee, DateTime.UtcNow);
    }

    public async Task RefundAsync(string code, Guid customerId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return;

        var voucher = await vouchers.GetByCodeAsync(code.Trim().ToUpperInvariant(), ct);
        if (voucher is null)
            return;

        voucher.Refund(customerId);
    }
}
