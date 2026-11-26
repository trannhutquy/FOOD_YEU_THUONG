using FoodHub.Application.Abstractions;
using FoodHub.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Shipper> Shippers => Set<Shipper>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Week 6 scope: exclude order/delivery graph from EF model to avoid unmapped value-object issues.
        modelBuilder.Ignore<Order>();
        modelBuilder.Ignore<OrderItem>();
        modelBuilder.Ignore<Delivery>();
        modelBuilder.Ignore<DeliveryCode>();
        modelBuilder.Ignore<Address>();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await base.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException($"Concurrency conflict while saving changes: {ex.Message}");
        }
    }

    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken, bool _)
        => SaveChangesAsync(cancellationToken);
}
