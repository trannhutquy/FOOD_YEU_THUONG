using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FoodHub.Application.Abstractions;
using FoodHub.Domain;
using Microsoft.IdentityModel.Tokens;

namespace FoodHub.Infrastructure.Auth;

public class JwtTokenService(JwtOptions options) : ITokenService
{
    public string CreateAccessToken(User user)
    {
        // Dùng tên claim ngắn "sub", "email", "role" để không phụ thuộc cơ chế đổi tên claim của từng phiên bản
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(options.AccessMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string Plain, RefreshToken Entity) CreateRefreshToken(Guid userId)
    {
        // 64 byte ngẫu nhiên an toàn; chuỗi gốc chỉ trả cho client, cơ sở dữ liệu chỉ giữ bản băm
        var plain = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var entity = new RefreshToken(userId, HashRefreshToken(plain),
                                      DateTime.UtcNow.AddDays(options.RefreshDays));
        return (plain, entity);
    }

    public string HashRefreshToken(string plain) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plain)));
}
