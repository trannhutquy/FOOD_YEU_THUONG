namespace FoodHub.Domain;

/// <summary>
/// Strategy contract for computing order discounts (e.g., percentage, fixed amount, or free shipping vouchers).
/// </summary>
public interface IDiscountStrategy
{
    Money CalculateDiscount(Money subtotal, Money shippingFee);
}
