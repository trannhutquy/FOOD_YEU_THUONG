using System.ComponentModel.DataAnnotations;
using FoodHub.Domain;

namespace FoodHub.Application.Vouchers;

public record CreateVoucherRequest(
    [Required, StringLength(50, MinimumLength = 1)] string Code,
    VoucherType Type,
    [Range(0, double.MaxValue)] decimal Value,
    decimal? MaxDiscount,
    [Range(0, double.MaxValue)] decimal MinOrderValue,
    DateTime ExpiresAtUtc,
    [Range(1, int.MaxValue)] int UsageLimit);

public record VoucherDto(
    Guid Id,
    string Code,
    VoucherType Type,
    decimal Value,
    decimal? MaxDiscount,
    decimal MinOrderValue,
    DateTime ExpiresAtUtc,
    int UsageLimit,
    int UsedCount)
{
    public static VoucherDto From(Voucher voucher) =>
        new(
            voucher.Id,
            voucher.Code,
            voucher.Type,
            voucher.Value,
            voucher.MaxDiscount,
            voucher.MinOrderValue,
            voucher.ExpiresAtUtc,
            voucher.UsageLimit,
            voucher.UsedCount);
}

public record VoucherValidationDto(
    string Code,
    VoucherType Type,
    decimal DiscountAmount,
    decimal FinalTotal);
