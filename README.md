# FOOD_YEU_THUONG — ĐT18 FoodHub

Hệ thống đặt đồ ăn FoodHub (học phần IN4211 – Phát triển phần mềm hướng đối tượng).

## Swagger & Kiểm thử API
Khi chạy ở môi trường `Development` (bao gồm Docker Compose), mở Swagger UI tại:
- `http://localhost:8080/swagger` (Docker) hoặc `http://localhost:5037/swagger` (`dotnet run --project src/FoodHub.Api`).

Dùng nút **Authorize** trên Swagger UI và dán **Access Token** (JWT) nhận được từ `POST /api/auth/login` hoặc `POST /api/auth/register` để thử các API yêu cầu đăng nhập và phân quyền theo vai trò (`Customer`, `Restaurant`, `Shipper`, `Admin`).

## Thiết lập biến môi trường & Cơ sở dữ liệu (PostgreSQL)
1. Tạo tệp `.env` từ `.env.example` (đặt `DB_PASSWORD` và `JWT_KEY` dài tối thiểu 32 ký tự).
2. Chạy database và áp dụng EF Core migration:
   ```powershell
   docker compose up db -d
   $env:ConnectionStrings__Default = "Host=localhost;Port=5432;Database=foodhub;Username=foodhub;Password=<DB_PASSWORD>"
   $env:Jwt__Key = "<JWT_KEY_AT_LEAST_32_CHARS>"
   dotnet ef database update -p src/FoodHub.Infrastructure -s src/FoodHub.Api
   ```
3. Tài liệu thiết kế CSDL phần xác thực, quán/menu: [docs/er-draft.md](docs/er-draft.md).

## Build & Kiểm thử tự động
```powershell
dotnet build
dotnet test --collect:"XPlat Code Coverage"
```
