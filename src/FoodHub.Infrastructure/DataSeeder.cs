using FoodHub.Domain;

namespace FoodHub.Infrastructure;

/// <summary>
/// Centralized Data Seeder providing sample accounts, restaurants, menu items, shippers, orders, and reviews.
/// Designed for local testing, integration tests, and Swagger API demonstrations.
/// </summary>
public static class DataSeeder
{
    private static readonly DateTime BaseTime = new(2026, 11, 26, 8, 0, 0, DateTimeKind.Utc);

    // Constant Identifiers for Deterministic Seeding
    public static readonly Guid Restaurant1Id = Guid.Parse("22222222-2222-2222-2222-222222222201");
    public static readonly Guid Restaurant2Id = Guid.Parse("22222222-2222-2222-2222-222222222202");

    public static readonly Guid Shipper1Id = Guid.Parse("33333333-3333-3333-3333-333333333301");
    public static readonly Guid Shipper2Id = Guid.Parse("33333333-3333-3333-3333-333333333302");

    public static readonly Guid OrderDeliveredReviewedId = Guid.Parse("44444444-4444-4444-4444-444444444401");
    public static readonly Guid OrderDeliveredPendingReviewId = Guid.Parse("44444444-4444-4444-4444-444444444402");
    public static readonly Guid OrderDeliveringId = Guid.Parse("44444444-4444-4444-4444-444444444403");
    public static readonly Guid OrderPreparingId = Guid.Parse("44444444-4444-4444-4444-444444444404");
    public static readonly Guid OrderPendingId = Guid.Parse("44444444-4444-4444-4444-444444444405");

    public static readonly Guid SampleReviewId = Guid.Parse("55555555-5555-5555-5555-555555555501");

    // Pre-seeded Users
    public static IReadOnlyList<User> Users { get; } = SampleAccounts.All
        .Select(account => account.ToUser())
        .ToList();

    // Pre-seeded Restaurants
    public static readonly Restaurant RestaurantComTam = new(
        name: "Cơm Tấm Sen Hồng",
        ownerUserId: SampleAccounts.OwnerComTam.Id,
        isOpen: true,
        address: "123 Đường 30 Tháng 4, Phường 1, TP. Cao Lãnh, Đồng Tháp",
        id: Restaurant1Id);

    public static readonly Restaurant RestaurantBunBo = new(
        name: "Bún Bò Huế Xưa",
        ownerUserId: SampleAccounts.OwnerBunBo.Id,
        isOpen: true,
        address: "78 Lý Thường Kiệt, Phường 2, TP. Cao Lãnh, Đồng Tháp",
        id: Restaurant2Id);

    public static IReadOnlyList<Restaurant> Restaurants { get; } = new[]
    {
        RestaurantComTam,
        RestaurantBunBo
    };

    // Pre-seeded Menu Items
    public static readonly MenuItem ItemComTamSuonNuong = new(
        restaurantId: Restaurant1Id,
        name: "Cơm tấm sườn nướng đặc biệt",
        price: new Money(45000),
        isAvailable: true,
        id: Guid.Parse("66666666-6666-6666-6666-666666666601"));

    public static readonly MenuItem ItemComTamSuonBiCha = new(
        restaurantId: Restaurant1Id,
        name: "Cơm tấm sườn bì chả",
        price: new Money(50000),
        isAvailable: true,
        id: Guid.Parse("66666666-6666-6666-6666-666666666602"));

    public static readonly MenuItem ItemComTamGaNuong = new(
        restaurantId: Restaurant1Id,
        name: "Cơm tấm gà nướng mật ong",
        price: new Money(45000),
        isAvailable: true,
        id: Guid.Parse("66666666-6666-6666-6666-666666666603"));

    public static readonly MenuItem ItemCanhKhoQua = new(
        restaurantId: Restaurant1Id,
        name: "Canh khổ qua dồn thịt",
        price: new Money(15000),
        isAvailable: true,
        id: Guid.Parse("66666666-6666-6666-6666-666666666604"));

    public static readonly MenuItem ItemTraDa = new(
        restaurantId: Restaurant1Id,
        name: "Trà đá hoa lài",
        price: new Money(5000),
        isAvailable: true,
        id: Guid.Parse("66666666-6666-6666-6666-666666666605"));

    public static readonly MenuItem ItemBunBoBapHoa = new(
        restaurantId: Restaurant2Id,
        name: "Bún bò huế bắp hoa",
        price: new Money(50000),
        isAvailable: true,
        id: Guid.Parse("66666666-6666-6666-6666-666666666606"));

    public static readonly MenuItem ItemBunBoDacBiet = new(
        restaurantId: Restaurant2Id,
        name: "Bún bò huế đặc biệt (giò, nạm, chả cua)",
        price: new Money(65000),
        isAvailable: true,
        id: Guid.Parse("66666666-6666-6666-6666-666666666607"));

    public static readonly MenuItem ItemNuocSam = new(
        restaurantId: Restaurant2Id,
        name: "Nước sâm thốt nốt",
        price: new Money(15000),
        isAvailable: true,
        id: Guid.Parse("66666666-6666-6666-6666-666666666608"));

    public static IReadOnlyList<MenuItem> MenuItems { get; } = new[]
    {
        ItemComTamSuonNuong,
        ItemComTamSuonBiCha,
        ItemComTamGaNuong,
        ItemCanhKhoQua,
        ItemTraDa,
        ItemBunBoBapHoa,
        ItemBunBoDacBiet,
        ItemNuocSam
    };

    // Pre-seeded Shippers (holding currently assigned active orders where applicable)
    public static readonly Shipper ShipperNhanh = Shipper.Restore(
        id: Shipper1Id,
        userId: SampleAccounts.ShipperNhanh.Id,
        isAvailable: true,
        version: 1,
        activeOrderIds: new[] { OrderDeliveringId }); // Currently holds 1 active order

    public static readonly Shipper ShipperTocDo = Shipper.Restore(
        id: Shipper2Id,
        userId: SampleAccounts.ShipperTocDo.Id,
        isAvailable: true,
        version: 1,
        activeOrderIds: Array.Empty<Guid>());

    public static IReadOnlyList<Shipper> Shippers { get; } = new[]
    {
        ShipperNhanh,
        ShipperTocDo
    };

    // Pre-seeded Orders covering all business stages
    public static readonly Order OrderDeliveredReviewed = Order.Restore(
        id: OrderDeliveredReviewedId,
        customerId: SampleAccounts.CustomerThang.Id,
        restaurantId: Restaurant1Id,
        status: OrderStatus.Delivered,
        shipperId: Shipper1Id,
        deliveryAddress: new Address("15 Nguyễn Huệ", "Phường 1", "TP. Cao Lãnh", "Đồng Tháp", "Giao cổng trước"),
        shippingFee: new Money(15000),
        discountAmount: Money.Zero,
        appliedVoucherCode: null,
        isReviewed: true,
        createdAtUtc: BaseTime.AddMinutes(-120),
        items: new[]
        {
            new OrderItem(ItemComTamSuonBiCha.Id, ItemComTamSuonBiCha.Name, ItemComTamSuonBiCha.Price, 2),
            new OrderItem(ItemTraDa.Id, ItemTraDa.Name, ItemTraDa.Price, 2)
        });

    public static readonly Order OrderDeliveredPendingReview = Order.Restore(
        id: OrderDeliveredPendingReviewId,
        customerId: SampleAccounts.CustomerThang.Id,
        restaurantId: Restaurant2Id,
        status: OrderStatus.Delivered,
        shipperId: Shipper2Id,
        deliveryAddress: new Address("15 Nguyễn Huệ", "Phường 1", "TP. Cao Lãnh", "Đồng Tháp"),
        shippingFee: new Money(15000),
        discountAmount: Money.Zero,
        appliedVoucherCode: null,
        isReviewed: false, // Ready for reviewing in Swagger / tests!
        createdAtUtc: BaseTime.AddMinutes(-60),
        items: new[]
        {
            new OrderItem(ItemBunBoDacBiet.Id, ItemBunBoDacBiet.Name, ItemBunBoDacBiet.Price, 1),
            new OrderItem(ItemNuocSam.Id, ItemNuocSam.Name, ItemNuocSam.Price, 1)
        });

    public static readonly Order OrderDelivering = Order.Restore(
        id: OrderDeliveringId,
        customerId: SampleAccounts.CustomerVy.Id,
        restaurantId: Restaurant1Id,
        status: OrderStatus.Delivering,
        shipperId: Shipper1Id,
        deliveryAddress: new Address("20 Lý Thường Kiệt", "Phường 2", "TP. Cao Lãnh", "Đồng Tháp"),
        shippingFee: new Money(15000),
        discountAmount: Money.Zero,
        appliedVoucherCode: null,
        isReviewed: false,
        createdAtUtc: BaseTime.AddMinutes(-30),
        items: new[]
        {
            new OrderItem(ItemComTamGaNuong.Id, ItemComTamGaNuong.Name, ItemComTamGaNuong.Price, 1)
        });

    public static readonly Order OrderPreparing = Order.Restore(
        id: OrderPreparingId,
        customerId: SampleAccounts.CustomerQuy.Id,
        restaurantId: Restaurant2Id,
        status: OrderStatus.Preparing,
        shipperId: null,
        deliveryAddress: new Address("88 Điện Biên Phủ", "Phường Mỹ Phú", "TP. Cao Lãnh", "Đồng Tháp"),
        shippingFee: new Money(20000),
        discountAmount: Money.Zero,
        appliedVoucherCode: null,
        isReviewed: false,
        createdAtUtc: BaseTime.AddMinutes(-15),
        items: new[]
        {
            new OrderItem(ItemBunBoBapHoa.Id, ItemBunBoBapHoa.Name, ItemBunBoBapHoa.Price, 2)
        });

    public static readonly Order OrderPending = Order.Restore(
        id: OrderPendingId,
        customerId: SampleAccounts.CustomerThang.Id,
        restaurantId: Restaurant1Id,
        status: OrderStatus.Pending,
        shipperId: null,
        deliveryAddress: new Address("15 Nguyễn Huệ", "Phường 1", "TP. Cao Lãnh", "Đồng Tháp"),
        shippingFee: new Money(15000),
        discountAmount: Money.Zero,
        appliedVoucherCode: null,
        isReviewed: false,
        createdAtUtc: BaseTime.AddMinutes(-5),
        items: new[]
        {
            new OrderItem(ItemComTamSuonNuong.Id, ItemComTamSuonNuong.Name, ItemComTamSuonNuong.Price, 1)
        });

    public static IReadOnlyList<Order> Orders { get; } = new[]
    {
        OrderDeliveredReviewed,
        OrderDeliveredPendingReview,
        OrderDelivering,
        OrderPreparing,
        OrderPending
    };

    // Pre-seeded Review for the delivered order
    public static readonly Review SampleReview = Review.Restore(
        id: SampleReviewId,
        orderId: OrderDeliveredReviewedId,
        customerId: SampleAccounts.CustomerThang.Id,
        restaurantId: Restaurant1Id,
        shipperId: Shipper1Id,
        restaurantRating: 5,
        restaurantComment: "Cơm tấm hạt dẻo thơm, sườn ướp đậm đà chuẩn vị miền Tây. Đóng gói sạch sẽ!",
        shipperRating: 5,
        shipperComment: "Shipper giao rất nhanh, đồ ăn đến nơi vẫn còn nóng hổi, bác tài cực kỳ lịch sự.",
        createdAtUtc: BaseTime.AddMinutes(-90));

    public static IReadOnlyList<Review> Reviews { get; } = new[]
    {
        SampleReview
    };

    /// <summary>
    /// Factory creating a pre-seeded InMemoryReviewStore.
    /// </summary>
    public static InMemoryReviewStore CreateSeededReviewStore() => new(Reviews);

    /// <summary>
    /// Factory creating a pre-seeded InMemoryShipperStore.
    /// </summary>
    public static InMemoryShipperStore CreateSeededShipperStore() => new(Shippers);

    /// <summary>
    /// Produces a seed overview summary dictionary.
    /// </summary>
    public static object GetSummary() => new
    {
        UserCount = Users.Count,
        RestaurantCount = Restaurants.Count,
        MenuItemCount = MenuItems.Count,
        ShipperCount = Shippers.Count,
        OrderCount = Orders.Count,
        ReviewCount = Reviews.Count,
        DeliveredOrdersReadyToReview = Orders.Count(o => o.Status == OrderStatus.Delivered && !o.IsReviewed),
        SampleAccounts = SampleAccounts.All.Select(a => new
        {
            a.Id,
            a.Email,
            a.RawPassword,
            a.FullName,
            a.Role,
            a.PhoneNumber,
            a.Description
        })
    };
}
