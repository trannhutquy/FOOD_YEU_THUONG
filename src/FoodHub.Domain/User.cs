namespace FoodHub.Domain;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private User() { }

    public User(
        string email,
        string passwordHash,
        string fullName,
        string role,
        string? phoneNumber = null,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Full name is required.");
        if (string.IsNullOrWhiteSpace(role))
            throw new DomainException("Role is required.");

        var normalizedRole = UserRoles.ToName(UserRoles.Parse(role));

        Id = id ?? Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        Role = normalizedRole;
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
    }

    public User(string email, string passwordHash, string fullName, UserRole role)
        : this(email, passwordHash, fullName, UserRoles.ToName(role))
    {
    }

    public UserRole RoleEnum => UserRoles.Parse(Role);
}
