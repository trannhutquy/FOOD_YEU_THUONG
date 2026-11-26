# Sơ đồ ER: phần của Vy (Xác thực, Quản lý quán, Voucher)

Tên cột khớp với mã nguồn (EF Core). Bảng `shippers` do Quy sở hữu (Vy chỉ tạo bản ghi lúc đăng ký);
bảng `orders` của Quy sẽ bổ sung sau. Hai bảng voucher được tạo ở tuần 9.

```mermaid
erDiagram
    users ||--o{ refresh_tokens : "có nhiều"
    users ||--o{ restaurants : "sở hữu (OwnerUserId)"
    users ||--o| shippers : "là (UserId, của Quy)"
    restaurants ||--o{ menu_items : "có nhiều"
    vouchers ||--o{ voucher_redemptions : "được dùng"
    users ||--o{ voucher_redemptions : "khách đã dùng"

    users {
        uuid Id PK
        varchar Email UK
        varchar PasswordHash
        varchar FullName
        varchar Role "Customer, Restaurant, Shipper, Admin"
    }
    refresh_tokens {
        uuid Id PK
        uuid UserId FK
        varchar TokenHash UK "chỉ lưu bản băm SHA-256"
        timestamptz ExpiresAtUtc
        timestamptz RevokedAtUtc "null nếu còn hiệu lực"
        varchar ReplacedByTokenHash "token thay thế khi xoay vòng"
    }
    restaurants {
        uuid Id PK
        uuid OwnerUserId FK
        varchar Name
        boolean IsOpen
    }
    menu_items {
        uuid Id PK
        uuid RestaurantId FK
        varchar Name
        numeric Price "Money.Amount"
        boolean IsAvailable
    }
    vouchers {
        uuid Id PK
        varchar Code UK "chữ HOA"
        varchar Type "Percentage, FixedAmount, FreeShipping"
        numeric Value
        numeric MaxDiscount "null nếu không có trần"
        numeric MinOrderValue
        timestamptz ExpiresAtUtc
        integer UsageLimit
        integer UsedCount
        bigint Version "concurrency token"
    }
    voucher_redemptions {
        uuid VoucherId PK,FK
        uuid CustomerId PK,FK
        timestamptz RedeemedAtUtc
    }
    shippers {
        uuid Id PK
        uuid UserId FK,UK
        boolean IsAvailable
        integer Version "concurrency token (của Quy)"
    }
```
