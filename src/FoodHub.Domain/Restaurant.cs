namespace FoodHub.Domain;

public class Restaurant
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public bool IsOpen { get; private set; }
    public string? Address { get; private set; }

    public Restaurant(string name, Guid ownerUserId, bool isOpen = true, string? address = null, Guid? id = null)
    {
        if (id.HasValue && id.Value != Guid.Empty)
            Id = id.Value;
        Name = name;
        OwnerUserId = ownerUserId;
        IsOpen = isOpen;
        Address = address;
    }

    public void SetOpen(bool isOpen) => IsOpen = isOpen;
}