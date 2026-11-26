using System.Security.Claims;
using FoodHub.Application.Abstractions;
using FoodHub.Domain;

namespace FoodHub.Api.Auth;

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid Id
    {
        get
        {
            // Thử cả hai kiểu tên claim phòng khi phiên bản đổi tên "sub"
            var raw = Principal?.FindFirst("sub")?.Value
                      ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(raw, out var id)
                ? id
                : throw new UnauthorizedException("Not authenticated.");
        }
    }

    public UserRole Role
    {
        get
        {
            var raw = Principal?.FindFirst("role")?.Value
                      ?? Principal?.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(raw, out var role)
                ? role
                : throw new UnauthorizedException("Not authenticated.");
        }
    }
}
