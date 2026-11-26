namespace FoodHub.Domain;

public class RefreshToken
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        if (userId == Guid.Empty) throw new DomainException("User is required.");
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new DomainException("Token hash is required.");

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void Revoke(DateTime nowUtc, string? replacedByHash = null)
    {
        RevokedAtUtc = nowUtc;
        ReplacedByTokenHash = replacedByHash;
    }
}
