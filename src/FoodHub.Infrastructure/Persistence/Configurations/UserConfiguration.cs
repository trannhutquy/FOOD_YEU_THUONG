using FoodHub.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users", t =>
            t.HasCheckConstraint("CK_users_Role", "\"Role\" IN ('Customer', 'Restaurant', 'Shipper', 'Admin')"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).IsRequired().HasMaxLength(256);
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.PasswordHash).IsRequired().HasMaxLength(100);
        b.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Role).IsRequired().HasMaxLength(20);
        b.Property(x => x.PhoneNumber).HasMaxLength(30);
        b.Property(x => x.CreatedAtUtc);
    }
}
