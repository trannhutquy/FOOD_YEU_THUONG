using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FoodHub.Infrastructure.Persistence;

// Chỉ dùng khi chạy lệnh "dotnet ef ...": tạo DbContext mà không cần chạy Program.cs.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=foodhub;Username=foodhub;Password=change_me";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .Options;
        return new AppDbContext(options);
    }
}
