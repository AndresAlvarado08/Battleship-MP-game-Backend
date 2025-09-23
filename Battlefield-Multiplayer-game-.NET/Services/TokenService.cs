using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Battlefield_Multiplayer_game_.NET.Services;

public class TokenService : ITokenServices
{

    private readonly string _secret;
    private readonly string? _issuer;
    private readonly string? _audience;
    private readonly int _expireMinutes;

    public TokenService(IConfiguration config)
    {
        _secret = Environment.GetEnvironmentVariable("JWT_KEY") ?? config["Jwt:Key"]!; // 🔹 variable de entorno
        _issuer = config["Jwt:Issuer"];       // 🔹 URL backend
        _audience = config["Jwt:Audience"];   // 🔹 URL frontend
        _expireMinutes = int.Parse(config["Jwt:ExpireMinutes"] ?? "15"); // 🔹 duración token
    }

    public string GenerateAccessToken(string username)
    {
        var claims = new[]
        {
                new Claim(ClaimTypes.Name, username),
                new Claim("role", "player")
            };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,          // 🔹 no cambiar
            audience: _audience,      // 🔹 no cambiar
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expireMinutes), // 🔹 duración
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()); // 🔹 token aleatorio
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _audience,  // 🔹 coincidir con JSON
            ValidateIssuer = true,
            ValidIssuer = _issuer,      // 🔹 coincidir con JSON
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
            ValidateLifetime = false    // 🔹 permitir tokens expirados para refresh
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
        return principal;
    }
}

