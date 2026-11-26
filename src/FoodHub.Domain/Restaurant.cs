namespace FoodHub.Domain;

public class Restaurant
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public Guid OwnerUserId { get; private set; }
    public bool IsOpen { get; private set; }
    public string? Address { get; private set; }

    private Restaurant() { }

    public Restaurant(string name, Guid ownerUserId, bool isOpen = true, string? address = null, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Restaurant name is required.");
        if (ownerUserId == Guid.Empty)
            throw new DomainException("Owner user is required.");

        if (id.HasValue && id.Value != Guid.Empty)
            Id = id.Value;
        Name = name.Trim();
        OwnerUserId = ownerUserId;
        IsOpen = isOpen;
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Restaurant name is required.");
        Name = name.Trim();
    }

    public void SetOpen(bool isOpen) => IsOpen = isOpen;
}
