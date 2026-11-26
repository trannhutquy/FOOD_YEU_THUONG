namespace FoodHub.Domain;

public class Restaurant
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public bool IsOpen { get; private set; }

    private Restaurant()
    {
        Name = null!;
    }

    public Restaurant(string name, Guid ownerUserId, bool isOpen = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Restaurant name is required.");
        if (ownerUserId == Guid.Empty)
            throw new DomainException("Owner user is required.");

        Name = name.Trim();
        OwnerUserId = ownerUserId;
        IsOpen = isOpen;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Restaurant name is required.");

        Name = name.Trim();
    }

    public void SetOpen(bool isOpen) => IsOpen = isOpen;
}
