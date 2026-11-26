namespace FoodHub.Domain;

public enum UserRole { Customer, Restaurant, Shipper, Admin }

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FullName { get; private set; }
    public UserRole Role { get; private set; }

    public User(string email, string passwordHash, string fullName, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");
        if (string.IsNullOrWhiteSpace(fullName)) throw new DomainException("Full name is required.");

        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        Role = role;
    }
}
