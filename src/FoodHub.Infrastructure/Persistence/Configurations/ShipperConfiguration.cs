using FoodHub.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodHub.Infrastructure.Persistence.Configurations;

// BẢN TẠM để bảng shippers tồn tại cho đăng ký Shipper.
// Việc lưu ActiveOrderIds là phần của Quy: khi Quy thêm cấu hình Shipper chính thức thì XOÁ tệp này (tránh khai báo hai lần).
public class ShipperConfiguration : IEntityTypeConfiguration<Shipper>
{
    public void Configure(EntityTypeBuilder<Shipper> b)
    {
        b.ToTable("shippers");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId).IsUnique();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.Ignore(x => x.ActiveOrderIds);
        b.Ignore(x => x.ActiveOrderCount);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
