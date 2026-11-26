namespace FoodHub.Domain;

public class VoucherRedemption
{
    public Guid VoucherId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTime RedeemedAtUtc { get; private set; }

    private VoucherRedemption() { }

    public VoucherRedemption(Guid voucherId, Guid customerId, DateTime redeemedAtUtc)
    {
        if (voucherId == Guid.Empty)
            throw new DomainException("Voucher id is required.");
        if (customerId == Guid.Empty)
            throw new DomainException("Customer id is required.");

        VoucherId = voucherId;
        CustomerId = customerId;
        RedeemedAtUtc = redeemedAtUtc;
    }
}

public class Voucher
{
    private readonly List<VoucherRedemption> _redemptions = new();

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Code { get; private set; }
    public VoucherType Type { get; private set; }
    public decimal Value { get; private set; }
    public decimal? MaxDiscount { get; private set; }
    public decimal MinOrderValue { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public int UsageLimit { get; private set; }
    public int UsedCount { get; private set; }
    public long Version { get; private set; }

    public IReadOnlyCollection<VoucherRedemption> Redemptions => _redemptions.AsReadOnly();
    public IReadOnlyList<Guid> RedeemedBy => _redemptions.Select(x => x.CustomerId).ToList();

    private Voucher()
    {
        Code = null!;
    }

    public Voucher(
        string code,
        VoucherType type,
        decimal value,
        decimal? maxDiscount,
        decimal minOrderValue,
        DateTime expiresAtUtc,
        int usageLimit)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Voucher code is required.");
        if (value < 0)
            throw new DomainException("Voucher value cannot be negative.");
        if (value <= 0 && type != VoucherType.FreeShipping)
            throw new DomainException("Voucher value must be greater than zero.");
        if (type == VoucherType.Percentage && value > 100)
            throw new DomainException("Percentage cannot exceed 100.");
        if (maxDiscount is <= 0)
            throw new DomainException("Max discount must be greater than zero.");
        if (minOrderValue < 0)
            throw new DomainException("Minimum order value cannot be negative.");
        if (usageLimit <= 0)
            throw new DomainException("Usage limit must be greater than zero.");

        Code = code.Trim().ToUpperInvariant();
        Type = type;
        Value = value;
        MaxDiscount = maxDiscount;
        MinOrderValue = minOrderValue;
        ExpiresAtUtc = expiresAtUtc;
        UsageLimit = usageLimit;
    }

    public IDiscountStrategy CreateDiscountStrategy() =>
        DiscountStrategyFactory.Create(Type, Value, MaxDiscount);

    public Money Preview(Guid customerId, Money subtotal, Money shippingFee, DateTime nowUtc)
    {
        if (customerId == Guid.Empty)
            throw new DomainException("Customer is required.");
        if (nowUtc > ExpiresAtUtc)
            throw new DomainException("Voucher has expired.");
        if (UsedCount >= UsageLimit)
            throw new DomainException("Voucher usage limit reached.");
        if (_redemptions.Any(x => x.CustomerId == customerId))
            throw new DomainException("Voucher was already used by this customer.");
        if (subtotal.Amount < MinOrderValue)
            throw new DomainException("Order value is below the voucher minimum.");

        return CreateDiscountStrategy().CalculateDiscount(subtotal, shippingFee);
    }

    public Money Redeem(Guid customerId, Money subtotal, Money shippingFee, DateTime nowUtc)
    {
        var discount = Preview(customerId, subtotal, shippingFee, nowUtc);
        UsedCount++;
        _redemptions.Add(new VoucherRedemption(Id, customerId, nowUtc));
        Version++;
        return discount;
    }

    public void Refund(Guid customerId)
    {
        var existing = _redemptions.FirstOrDefault(x => x.CustomerId == customerId);
        if (existing is null)
            return;

        _redemptions.Remove(existing);
        if (UsedCount > 0)
            UsedCount--;
        Version++;
    }
}
