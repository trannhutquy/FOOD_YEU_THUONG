namespace FoodHub.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface IDeliveryCodeProtector
{
    string Protect(string plainCode);
    string Unprotect(string protectedCode);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
