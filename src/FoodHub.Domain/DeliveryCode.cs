using System.Security.Cryptography;
using System.Text;

namespace FoodHub.Domain;

/// <summary>
/// Represents a single-use, time-limited 6-digit delivery confirmation code stored as a salted SHA-256 hash.
/// </summary>
public class DeliveryCode
{
    public const int CodeLength = 6;
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(2);

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string CodeHash { get; private set; } = string.Empty;
    public string Salt { get; private set; } = string.Empty;
    public string? ProtectedCode { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public int FailedAttempts { get; private set; }
    public bool IsLocked { get; private set; }
    public bool IsUsed => UsedAtUtc.HasValue;

    private DeliveryCode() { }

    public static (DeliveryCode Code, string PlainCode) Create(DateTime utcNow, TimeSpan? ttl = null)
    {
        var lifetime = ttl ?? DefaultTtl;
        if (lifetime <= TimeSpan.Zero)
            throw new DomainException("Delivery code TTL must be positive.");

        var entity = new DeliveryCode();
        var plainCode = entity.AssignNewCode(utcNow, lifetime);
        return (entity, plainCode);
    }

    public void SetProtectedCode(string protectedCode)
    {
        if (string.IsNullOrWhiteSpace(protectedCode))
            throw new DomainException("Protected code cannot be empty.");
        ProtectedCode = protectedCode;
    }

    public void VerifyOrThrow(string candidateCode, DateTime utcNow)
    {
        if (IsUsed)
            throw new DomainException("Delivery code has already been used.");
        if (IsLocked)
            throw new DomainException("Delivery code is locked due to too many failed attempts.");
        if (utcNow > ExpiresAtUtc)
            throw new DomainException("Delivery code has expired.");

        var normalized = candidateCode?.Trim() ?? string.Empty;
        if (normalized.Length == CodeLength && normalized.All(char.IsAsciiDigit) && MatchesHash(normalized))
        {
            UsedAtUtc = utcNow;
            return;
        }

        FailedAttempts++;
        if (FailedAttempts >= MaxFailedAttempts)
        {
            IsLocked = true;
            throw new DomainException($"Delivery code is locked after {MaxFailedAttempts} failed attempts.");
        }

        throw new DomainException($"Invalid delivery code. {MaxFailedAttempts - FailedAttempts} attempt(s) remaining.");
    }

    public string Reset(DateTime utcNow, TimeSpan? ttl = null)
    {
        if (IsUsed)
            throw new DomainException("Cannot reset a delivery code that has already been used.");

        var lifetime = ttl ?? DefaultTtl;
        if (lifetime <= TimeSpan.Zero)
            throw new DomainException("Delivery code TTL must be positive.");

        return AssignNewCode(utcNow, lifetime);
    }

    private string AssignNewCode(DateTime utcNow, TimeSpan lifetime)
    {
        var plainCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var saltBytes = RandomNumberGenerator.GetBytes(16);

        Salt = Convert.ToBase64String(saltBytes);
        CodeHash = ComputeHash(plainCode, saltBytes);
        ProtectedCode = null;
        CreatedAtUtc = utcNow;
        ExpiresAtUtc = utcNow.Add(lifetime);
        FailedAttempts = 0;
        IsLocked = false;

        return plainCode;
    }

    private bool MatchesHash(string candidateCode)
    {
        var saltBytes = Convert.FromBase64String(Salt);
        var expectedBytes = Convert.FromBase64String(CodeHash);
        var actualBytes = ComputeHashBytes(candidateCode, saltBytes);
        return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }

    private static string ComputeHash(string plainCode, byte[] saltBytes)
        => Convert.ToBase64String(ComputeHashBytes(plainCode, saltBytes));

    private static byte[] ComputeHashBytes(string plainCode, byte[] saltBytes)
    {
        var codeBytes = Encoding.UTF8.GetBytes(plainCode);
        var combined = new byte[saltBytes.Length + codeBytes.Length];
        Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
        Buffer.BlockCopy(codeBytes, 0, combined, saltBytes.Length, codeBytes.Length);
        return SHA256.HashData(combined);
    }
}
