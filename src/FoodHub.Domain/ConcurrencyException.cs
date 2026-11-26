namespace FoodHub.Domain;

/// <summary>
/// Raised when an optimistic concurrency check fails (e.g., concurrent shipper assignment).
/// </summary>
public class ConcurrencyException : DomainException
{
    public ConcurrencyException(string message) : base(message) { }
}
