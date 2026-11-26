using FoodHub.Domain;

namespace FoodHub.Application.Auth;

public record RegisterRequest(string Email, string Password, string FullName,
                              UserRole Role, string? RestaurantName);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record AuthResult(string AccessToken, string RefreshToken);
public record MeDto(Guid Id, UserRole Role);
