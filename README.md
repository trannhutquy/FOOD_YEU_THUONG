# FOOD_YEU_THUONG – FoodHub (ĐT18)

> **Học phần:** IN4211 – Phát triển phần mềm hướng đối tượng  
> **Học kỳ:** HK1, Năm học 2026 – 2027  
> **Nhóm thực hiện (Nhóm 3 thành viên):**  
> - **Lê Hà Hữu Thắng** (Nhóm trưởng)  
> - **Nguyễn Thuý Vy** (Nhóm phó)  
> - **Trần Nhựt Quy** (Thành viên)  

---

## 1. Giới thiệu đề tài
Hệ thống giao đồ ăn trực tuyến **FoodHub** hỗ trợ khách hàng tìm kiếm quán ăn, đặt món ăn, áp dụng mã giảm giá và theo dõi đơn hàng do tài xế giao tận nơi.

### Quy tắc nghiệp vụ cốt lõi:
1. **Một đơn – Một quán:** Mỗi đơn hàng chỉ chứa các món ăn thuộc cùng một quán ăn.
2. **Giới hạn nhận đơn của Shipper:** Mỗi tài xế (Shipper) nhận tối đa **2 đơn hàng cùng lúc** để đảm bảo thời gian giao và chất lượng món ăn.
3. **Xác nhận giao hàng an toàn:** Khách hàng đọc **mã 6 số bí mật** (DeliveryCode) cho tài xế để hoàn tất đơn hàng (`Delivered`).
4. **Đánh giá đơn hàng (Review):** Khách hàng chỉ có thể đánh giá quán ăn và tài xế khi đơn hàng đã ở trạng thái **Delivered**, và **mỗi đơn hàng chỉ được đánh giá đúng một lần**.

---

## 2. Kiến trúc giải pháp (.NET 10 Clean Architecture)
```text
src/
├── FoodHub.Domain/            # Lõi nghiệp vụ độc lập: Order, Delivery, Shipper, Review, User, Money
├── FoodHub.Application/       # Dịch vụ ứng dụng, Use Cases, DTOs
├── FoodHub.Infrastructure/    # DataSeeder, In-Memory Stores, Persistence Adapters
└── FoodHub.Api/               # ASP.NET Core Web API, Minimal Endpoints, OpenAPI / Swagger
tests/
├── FoodHub.Domain.Tests/      # Kiểm thử đơn vị tầng Domain (xUnit)
└── FoodHub.Api.Tests/         # Kiểm thử tích hợp và kiểm tra DataSeeder
```

---

## 3. Danh sách tài khoản mẫu (Sample Accounts)
Hệ thống cung cấp sẵn bộ tài khoản mẫu đầy đủ vai trò trong `SampleAccounts.cs` / `DataSeeder.cs`:

| Vai trò (Role) | Họ và tên | Email đăng nhập | Mật khẩu mẫu | Ghi chú |
|---|---|---|---|---|
| **Admin** | B | `admin@foodhub.com` | `AdminPassword123!` | Toàn quyền quản trị hệ thống |
| **RestaurantOwner** | lê Văn D | `owner.comtam@foodhub.com` | `OwnerPassword123!` | Chủ quán *Cơm Tấm Sen Hồng* |
| **RestaurantOwner** | nguyễn văn A | `owner.bunbo@foodhub.com` | `OwnerPassword123!` | Chủ quán *Bún Bò Huế Xưa* |
| **Shipper** | Lê Văn Nhanh | `shipper.nhanh@foodhub.com` | `ShipperPassword123!` | Tài xế 1 (đang nhận 1 đơn giao) |
| **Shipper** | phạm văn C | `shipper.tocdo@foodhub.com` | `ShipperPassword123!` | Tài xế 2 (sẵn sàng nhận đơn) |
| **Customer** | Lê Hà Hữu Thắng | `customer.thang@foodhub.com` | `CustomerPassword123!` | Khách hàng (Đã có đơn Delivered) |
| **Customer** | Nguyễn Thuý Vy | `customer.vy@foodhub.com` | `CustomerPassword123!` | Khách hàng (Đang có đơn Delivering) |
| **Customer** | Trần Nhựt Quy | `customer.quy@foodhub.com` | `CustomerPassword123!` | Khách hàng (Đang có đơn Preparing) |

---

## 4. Dữ liệu mẫu (DataSeeder)
Bộ dữ liệu khởi tạo sẵn phục vụ chạy thử và kiểm thử tự động:
- **Quán ăn & Thực đơn:**
  - *Cơm Tấm Sen Hồng* (5 món: Cơm sườn nướng, sườn bì chả, gà nướng mật ong, canh khổ qua, trà đá).
  - *Bún Bò Huế Xưa* (3 món: Bún bò bắp hoa, bún bò đặc biệt, nước sâm thốt nốt).
- **Đơn hàng theo từng trạng thái:**
  - `OrderDeliveredReviewed`: Đơn đã giao và đã được đánh giá (5 sao quán, 5 sao tài xế).
  - `OrderDeliveredPendingReview`: Đơn đã giao nhưng **chưa đánh giá** (sẵn sàng để kiểm thử chức năng Review).
  - `OrderDelivering`: Đơn đang được Shipper 1 giao đến khách hàng.
  - `OrderPreparing`: Đơn đang được chuẩn bị tại quán.
  - `OrderPending`: Đơn mới tạo chờ quán xác nhận.
- **Đánh giá mẫu:** Có sẵn đánh giá 5 sao cho đơn hàng đã giao thành công.

---

## 5. Hướng dẫn chạy & Kiểm thử

### Chạy toàn bộ kiểm thử đơn vị & kiểm thử bao phủ:
```powershell
# Chạy tất cả kiểm thử
dotnet test

# Chạy kiểm thử kèm đo độ bao phủ mã nguồn
dotnet test --collect:"XPlat Code Coverage"
```

### Khởi chạy ứng dụng Web API:
```powershell
dotnet run --project src/FoodHub.Api
```
Truy cập tài liệu API / OpenAPI tại:
- `https://localhost:7xxx/openapi/v1.json` (hoặc Swagger UI khi mở trình duyệt).

### Các endpoint kiểm tra nhanh dữ liệu:
- `GET /api/sample-accounts` – Xem danh sách tài khoản mẫu.
- `GET /api/seed/summary` – Xem thống kê dữ liệu mẫu đã nạp.
- `GET /api/reviews/restaurant/{restaurantId}` – Xem các đánh giá của quán.
- `GET /api/reviews/order/{orderId}` – Xem đánh giá của một đơn hàng.
