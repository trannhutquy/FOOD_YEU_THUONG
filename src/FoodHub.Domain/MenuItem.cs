namespace FoodHub.Domain;

public class MenuItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RestaurantId { get; private set; }
    public string Name { get; private set; }
    public Money Price { get; private set; }
    public bool IsAvailable { get; private set; }

    private MenuItem()
    {
        Name = null!;
    }

    public MenuItem(Guid restaurantId, string name, Money price, bool isAvailable = true)
    {
        if (restaurantId == Guid.Empty)
            throw new DomainException("Restaurant is required.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Menu item name is required.");
        if (price.Amount <= 0)
            throw new DomainException("Menu item price must be greater than zero.");

        RestaurantId = restaurantId;
        Name = name.Trim();
        Price = price;
        IsAvailable = isAvailable;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Menu item name is required.");

        Name = name.Trim();
    }

    public void ChangePrice(Money price)
    {
        if (price.Amount <= 0)
            throw new DomainException("Menu item price must be greater than zero.");

        Price = price;
    }

    public void SetAvailability(bool isAvailable) => IsAvailable = isAvailable;
}
