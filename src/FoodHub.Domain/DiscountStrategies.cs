namespace FoodHub.Domain;

public enum VoucherType
{
    Percentage,
    FixedAmount,
    FreeShipping
}

public class PercentageDiscount : IDiscountStrategy
{
    private readonly decimal _percent;
    private readonly decimal? _maxDiscount;

    public PercentageDiscount(decimal percent, decimal? maxDiscount = null)
    {
        if (percent is <= 0 or > 100)
            throw new DomainException("Percentage discount must be between 0 and 100.");
        if (maxDiscount is <= 0)
            throw new DomainException("Max discount must be greater than zero.");

        _percent = percent;
        _maxDiscount = maxDiscount;
    }

    public Money CalculateDiscount(Money subtotal, Money shippingFee)
    {
        var amount = Math.Round(subtotal.Amount * _percent / 100m, 0, MidpointRounding.AwayFromZero);
        if (_maxDiscount is not null)
            amount = Math.Min(amount, _maxDiscount.Value);

        return new Money(Math.Min(amount, subtotal.Amount));
    }
}

public class FixedAmountDiscount : IDiscountStrategy
{
    private readonly decimal _amount;

    public FixedAmountDiscount(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Fixed discount amount must be greater than zero.");

        _amount = amount;
    }

    public Money CalculateDiscount(Money subtotal, Money shippingFee) =>
        new(Math.Min(_amount, subtotal.Amount));
}

public class FreeShippingDiscount : IDiscountStrategy
{
    public Money CalculateDiscount(Money subtotal, Money shippingFee) => shippingFee;
}

public static class DiscountStrategyFactory
{
    public static IDiscountStrategy Create(VoucherType type, decimal value, decimal? maxDiscount) =>
        type switch
        {
            VoucherType.Percentage => new PercentageDiscount(value, maxDiscount),
            VoucherType.FixedAmount => new FixedAmountDiscount(value),
            VoucherType.FreeShipping => new FreeShippingDiscount(),
            _ => throw new DomainException("Unknown voucher type.")
        };
}
