using FoodHub.Domain;

namespace FoodHub.Infrastructure;

/// <summary>
/// Represents sample account credentials and role metadata for development and Swagger testing.
/// </summary>
public record SampleAccount(
    Guid Id,
    string Email,
    string RawPassword,
    string PasswordHash,
    string FullName,
    string Role,
    string PhoneNumber,
    string Description)
{
    public User ToUser() => new(Email, PasswordHash, FullName, Role, PhoneNumber, Id);
}

/// <summary>
/// Pre-configured sample user accounts covering every system role.
/// </summary>
public static class SampleAccounts
{
    // SHA256 simulation hashes for sample accounts
    private const string DefaultHash = "AQAAAAIAAYagAAAAEL+sampleHashedPasswordSecretString=";

    public static readonly SampleAccount Admin = new(
        Id: Guid.Parse("11111111-1111-1111-1111-111111111101"),
        Email: "admin@foodhub.com",
        RawPassword: "AdminPassword123!",
        PasswordHash: DefaultHash,
        FullName: "B",
        Role: UserRole.Admin,
        PhoneNumber: "0901000001",
        Description: "Tài khoản quản trị toàn quyền hệ thống");

    public static readonly SampleAccount OwnerComTam = new(
        Id: Guid.Parse("11111111-1111-1111-1111-111111111102"),
        Email: "owner.comtam@foodhub.com",
        RawPassword: "OwnerPassword123!",
        PasswordHash: DefaultHash,
        FullName: "lê Văn D",
        Role: UserRole.RestaurantOwner,
        PhoneNumber: "0901000002",
        Description: "Chủ quán Cơm Tấm Sen Hồng");

    public static readonly SampleAccount OwnerBunBo = new(
        Id: Guid.Parse("11111111-1111-1111-1111-111111111103"),
        Email: "owner.bunbo@foodhub.com",
        RawPassword: "OwnerPassword123!",
        PasswordHash: DefaultHash,
        FullName: "nguyễn văn A",
        Role: UserRole.RestaurantOwner,
        PhoneNumber: "0901000003",
        Description: "Chủ quán Bún Bò Huế Xưa");

    public static readonly SampleAccount ShipperNhanh = new(
        Id: Guid.Parse("11111111-1111-1111-1111-111111111104"),
        Email: "shipper.nhanh@foodhub.com",
        RawPassword: "ShipperPassword123!",
        PasswordHash: DefaultHash,
        FullName: "Lê Văn Nhanh",
        Role: UserRole.Shipper,
        PhoneNumber: "0901000004",
        Description: "Tài xế giao hàng tích cực (Shipper 1)");

    public static readonly SampleAccount ShipperTocDo = new(
        Id: Guid.Parse("11111111-1111-1111-1111-111111111105"),
        Email: "shipper.tocdo@foodhub.com",
        RawPassword: "ShipperPassword123!",
        PasswordHash: DefaultHash,
        FullName: "phạm văn C",
        Role: UserRole.Shipper,
        PhoneNumber: "0901000005",
        Description: "Tài xế giao hàng dự phòng (Shipper 2)");

    public static readonly SampleAccount CustomerThang = new(
        Id: Guid.Parse("11111111-1111-1111-1111-111111111106"),
        Email: "customer.thang@foodhub.com",
        RawPassword: "CustomerPassword123!",
        PasswordHash: DefaultHash,
        FullName: "Lê Hà Hữu Thắng",
        Role: UserRole.Customer,
        PhoneNumber: "0901000006",
        Description: "Khách hàng thân thiết (Trưởng nhóm Thắng)");

    public static readonly SampleAccount CustomerVy = new(
        Id: Guid.Parse("11111111-1111-1111-1111-111111111107"),
        Email: "customer.vy@foodhub.com",
        RawPassword: "CustomerPassword123!",
        PasswordHash: DefaultHash,
        FullName: "Nguyễn Thuý Vy",
        Role: UserRole.Customer,
        PhoneNumber: "0901000007",
        Description: "Khách hàng đặt món (Phó nhóm Vy)");

    public static readonly SampleAccount CustomerQuy = new(
        Id: Guid.Parse("11111111-1111-1111-1111-111111111108"),
        Email: "customer.quy@foodhub.com",
        RawPassword: "CustomerPassword123!",
        PasswordHash: DefaultHash,
        FullName: "Trần Nhựt Quy",
        Role: UserRole.Customer,
        PhoneNumber: "0901000008",
        Description: "Khách hàng đặt món (Thành viên Quy)");

    public static IReadOnlyList<SampleAccount> All { get; } = new[]
    {
        Admin,
        OwnerComTam,
        OwnerBunBo,
        ShipperNhanh,
        ShipperTocDo,
        CustomerThang,
        CustomerVy,
        CustomerQuy
    };
}
