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
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<VoucherRedemption> VoucherRedemptions => Set<VoucherRedemption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

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
}
