using FoodHub.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> b)
    {
        b.ToTable("menu_items", t =>
            t.HasCheckConstraint("CK_menu_items_Price", "\"Price\" > 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);

        // Money là Value Object: trong CSDL chỉ lưu số tiền (numeric), đọc lên bọc lại thành Money
        b.Property(x => x.Price)
            .HasConversion(m => m.Amount, v => new Money(v))
            .HasColumnType("numeric(18,2)");

        b.HasIndex(x => x.RestaurantId);
        b.HasOne<Restaurant>().WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Cascade);
    }
}
