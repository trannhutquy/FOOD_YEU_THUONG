namespace FoodHub.Domain;

public class MenuItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RestaurantId { get; private set; }
    public string Name { get; private set; }
    public Money Price { get; private set; }
    public bool IsAvailable { get; private set; }

    public MenuItem(Guid restaurantId, string name, Money price, bool isAvailable = true)
    {
        RestaurantId = restaurantId;
        Name = name;
        Price = price;
        IsAvailable = isAvailable;
    }
}