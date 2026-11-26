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
        b.HasIndex(x => x.Email).IsUnique();               // chặn trùng email ở cả mức cơ sở dữ liệu
        b.Property(x => x.PasswordHash).IsRequired().HasMaxLength(100);
        b.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
    }
}
