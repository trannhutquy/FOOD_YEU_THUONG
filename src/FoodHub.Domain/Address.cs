namespace FoodHub.Domain;

/// <summary>
/// Immutable value object representing a delivery address.
/// </summary>
public sealed record Address
{
    public string Street { get; }
    public string Ward { get; }
    public string District { get; }
    public string City { get; }
    public string? Note { get; }

    public Address(string street, string ward, string district, string city, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new DomainException("Street is required.");
        if (string.IsNullOrWhiteSpace(ward))
            throw new DomainException("Ward is required.");
        if (string.IsNullOrWhiteSpace(district))
            throw new DomainException("District is required.");
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("City is required.");

        Street = street.Trim();
        Ward = ward.Trim();
        District = district.Trim();
        City = city.Trim();
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
