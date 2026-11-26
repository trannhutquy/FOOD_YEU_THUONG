namespace FoodHub.Infrastructure.Auth;

public class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "foodhub";
    public string Audience { get; set; } = "foodhub-clients";
    public string Key { get; set; } = "";     // KHÔNG ghi vào appsettings; lấy từ biến môi trường Jwt__Key
    public int AccessMinutes { get; set; } = 15;
    public int RefreshDays { get; set; } = 7;
}
