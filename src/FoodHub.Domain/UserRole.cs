namespace FoodHub.Domain;

public enum UserRole
{
    Customer,
    Restaurant,
    Shipper,
    Admin
}

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
    public const string Restaurant = "Restaurant";
    public const string RestaurantOwner = "RestaurantOwner";
    public const string Shipper = "Shipper";

    public static string ToName(UserRole role) => role.ToString();

    public static UserRole Parse(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new DomainException("Role is required.");

        return role.Trim() switch
        {
            Admin => UserRole.Admin,
            Customer => UserRole.Customer,
            Restaurant or RestaurantOwner => UserRole.Restaurant,
            Shipper => UserRole.Shipper,
            _ => throw new DomainException($"Unknown role '{role}'.")
        };
    }
}
