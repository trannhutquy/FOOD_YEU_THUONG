using FoodHub.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations;

public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> b)
    {
        b.ToTable("vouchers", t =>
        {
            t.HasCheckConstraint("CK_vouchers_Type", "\"Type\" IN ('Percentage', 'FixedAmount', 'FreeShipping')");
            t.HasCheckConstraint("CK_vouchers_UsageLimit", "\"UsageLimit\" > 0");
            t.HasCheckConstraint("CK_vouchers_UsedCount", "\"UsedCount\" >= 0 AND \"UsedCount\" <= \"UsageLimit\"");
            t.HasCheckConstraint("CK_vouchers_Value", "\"Type\" = 'FreeShipping' OR \"Value\" > 0");
            t.HasCheckConstraint("CK_vouchers_Percent", "\"Type\" <> 'Percentage' OR \"Value\" <= 100");
            t.HasCheckConstraint("CK_vouchers_MinOrder", "\"MinOrderValue\" >= 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).IsRequired().HasMaxLength(50);
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Value).HasColumnType("numeric(18,2)");
        b.Property(x => x.MaxDiscount).HasColumnType("numeric(18,2)");
        b.Property(x => x.MinOrderValue).HasColumnType("numeric(18,2)");
        b.Property(x => x.Version).IsConcurrencyToken();

        b.Ignore(x => x.RedeemedBy);
        b.HasMany(x => x.Redemptions)
            .WithOne()
            .HasForeignKey(x => x.VoucherId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Redemptions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
