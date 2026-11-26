-- LƯỢC ĐỒ THAM KHẢO (PostgreSQL) cho PHẦN CỦA VY: Xác thực, Quản lý quán, Voucher.
-- Khớp với cấu hình EF Core trong src/FoodHub.Infrastructure/Persistence/Configurations.
-- NGUỒN CHÍNH THỨC là migration của EF (dotnet ef migrations add ...).
-- Dùng tệp này để xem / đưa vào báo cáo / kiểm tra; ĐỪNG chạy song song với migration trên cùng một CSDL.
--
-- Mục 1-4: tạo ở migration "AddAuthTables" (tuần 3).
-- Mục 5  : tạo ở migration "AddVouchers" (tuần 9).
-- Bảng shippers và orders thuộc phần của Quy, không nằm trong tệp này.

---------------------------------------------------------------- 1. users
CREATE TABLE users (
    "Id"           uuid          NOT NULL,
    "Email"        varchar(256)  NOT NULL,
    "PasswordHash" varchar(100)  NOT NULL,
    "FullName"     varchar(200)  NOT NULL,
    "Role"         varchar(20)   NOT NULL,
    CONSTRAINT "PK_users" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_users_Role" CHECK ("Role" IN ('Customer', 'Restaurant', 'Shipper', 'Admin'))
);
CREATE UNIQUE INDEX "IX_users_Email" ON users ("Email");

---------------------------------------------------------------- 2. refresh_tokens
CREATE TABLE refresh_tokens (
    "Id"                  uuid                      NOT NULL,
    "UserId"              uuid                      NOT NULL,
    "TokenHash"           varchar(128)              NOT NULL,   -- chỉ lưu bản băm, không lưu token gốc
    "ExpiresAtUtc"        timestamp with time zone  NOT NULL,
    "RevokedAtUtc"        timestamp with time zone  NULL,       -- NULL = còn hiệu lực
    "ReplacedByTokenHash" varchar(128)              NULL,       -- token thay thế khi xoay vòng
    CONSTRAINT "PK_refresh_tokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_refresh_tokens_users_UserId" FOREIGN KEY ("UserId")
        REFERENCES users ("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX "IX_refresh_tokens_TokenHash" ON refresh_tokens ("TokenHash");
CREATE INDEX "IX_refresh_tokens_UserId" ON refresh_tokens ("UserId");

---------------------------------------------------------------- 3. restaurants
CREATE TABLE restaurants (
    "Id"          uuid          NOT NULL,
    "Name"        varchar(200)  NOT NULL,
    "OwnerUserId" uuid          NOT NULL,   -- chủ quán; dùng cho phân quyền theo quyền sở hữu
    "IsOpen"      boolean       NOT NULL,
    CONSTRAINT "PK_restaurants" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_restaurants_users_OwnerUserId" FOREIGN KEY ("OwnerUserId")
        REFERENCES users ("Id") ON DELETE RESTRICT
);
CREATE INDEX "IX_restaurants_OwnerUserId" ON restaurants ("OwnerUserId");

---------------------------------------------------------------- 4. menu_items
CREATE TABLE menu_items (
    "Id"           uuid           NOT NULL,
    "RestaurantId" uuid           NOT NULL,
    "Name"         varchar(200)   NOT NULL,
    "Price"        numeric(18,2)  NOT NULL,   -- Money.Amount
    "IsAvailable"  boolean        NOT NULL,
    CONSTRAINT "PK_menu_items" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_menu_items_Price" CHECK ("Price" > 0),
    CONSTRAINT "FK_menu_items_restaurants_RestaurantId" FOREIGN KEY ("RestaurantId")
        REFERENCES restaurants ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_menu_items_RestaurantId" ON menu_items ("RestaurantId");

---------------------------------------------------------------- 5. vouchers (tuần 9)
CREATE TABLE vouchers (
    "Id"            uuid                      NOT NULL,
    "Code"          varchar(50)               NOT NULL,   -- luôn viết HOA
    "Type"          varchar(20)               NOT NULL,   -- Percentage | FixedAmount | FreeShipping
    "Value"         numeric(18,2)             NOT NULL,
    "MaxDiscount"   numeric(18,2)             NULL,       -- trần giảm (chủ yếu cho Percentage)
    "MinOrderValue" numeric(18,2)             NOT NULL,
    "ExpiresAtUtc"  timestamp with time zone  NOT NULL,
    "UsageLimit"    integer                   NOT NULL,
    "UsedCount"     integer                   NOT NULL,
    "Version"       bigint                    NOT NULL,   -- concurrency token: hai khách tranh lượt cuối thì chỉ một người thành công
    CONSTRAINT "PK_vouchers" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_vouchers_Type" CHECK ("Type" IN ('Percentage', 'FixedAmount', 'FreeShipping')),
    CONSTRAINT "CK_vouchers_UsageLimit" CHECK ("UsageLimit" > 0),
    CONSTRAINT "CK_vouchers_UsedCount" CHECK ("UsedCount" >= 0 AND "UsedCount" <= "UsageLimit"),
    CONSTRAINT "CK_vouchers_Value" CHECK ("Type" = 'FreeShipping' OR "Value" > 0),
    CONSTRAINT "CK_vouchers_Percent" CHECK ("Type" <> 'Percentage' OR "Value" <= 100),
    CONSTRAINT "CK_vouchers_MinOrder" CHECK ("MinOrderValue" >= 0)
);
CREATE UNIQUE INDEX "IX_vouchers_Code" ON vouchers ("Code");

-- Mỗi khách dùng một mã tối đa một lần: khoá chính ghép (VoucherId, CustomerId) bảo đảm điều đó ngay ở CSDL.
CREATE TABLE voucher_redemptions (
    "VoucherId"     uuid                      NOT NULL,
    "CustomerId"    uuid                      NOT NULL,
    "RedeemedAtUtc" timestamp with time zone  NOT NULL,
    CONSTRAINT "PK_voucher_redemptions" PRIMARY KEY ("VoucherId", "CustomerId"),
    CONSTRAINT "FK_voucher_redemptions_vouchers_VoucherId" FOREIGN KEY ("VoucherId")
        REFERENCES vouchers ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_voucher_redemptions_users_CustomerId" FOREIGN KEY ("CustomerId")
        REFERENCES users ("Id") ON DELETE RESTRICT
);
CREATE INDEX "IX_voucher_redemptions_CustomerId" ON voucher_redemptions ("CustomerId");
