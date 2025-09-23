using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Battlefield_Multiplayer_game_.NET.Configuration;
using Microsoft.Extensions.Options;
using Battlefield_Multiplayer_game_.NET.Models;

namespace Battlefield_Multiplayer_game_.NET.Services;

public class TokenService : ITokenServices
{
    private readonly JwtSettings _jwtSettings;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<TokenService> _logger;
    private readonly byte[] _key;

    public TokenService(IOptions<JwtSettings> jwtSettings, IRefreshTokenService refreshTokenService, ILogger<TokenService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
        
        var secretKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? _jwtSettings.Key;
        if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT Key debe tener al menos 32 caracteres");
        }
        _key = Encoding.UTF8.GetBytes(secretKey);
    }

    public string GenerateAccessToken(string username, string? role = null, Dictionary<string, string>? additionalClaims = null)
    {
        try
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, username),
                new(ClaimTypes.NameIdentifier, username),
                new("username", username),
                new("jti", Guid.NewGuid().ToString()),
                new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            if (additionalClaims != null)
            {
                foreach (var claim in additionalClaims)
                {
                    claims.Add(new Claim(claim.Key, claim.Value));
                }
            }

            var key = new SymmetricSecurityKey(_key);
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpireMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando access token para usuario {Username}", username);
            throw;
        }
    }

    public RefreshToken GenerateRefreshToken(string username, string? deviceInfo = null, string? ipAddress = null)
    {
        try
        {
            return _refreshTokenService.CreateRefreshTokenAsync(username, deviceInfo, ipAddress).Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando refresh token para usuario {Username}", username);
            throw;
        }
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        try
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(_key),
                ValidateLifetime = false,
                ClockSkew = TimeSpan.FromMinutes(_jwtSettings.ClockSkewMinutes)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validando token expirado");
            return null;
        }
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token)
    {
        try
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(_key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(_jwtSettings.ClockSkewMinutes)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validando token");
            return null;
        }
    }

    public bool ValidateRefreshToken(string token, string username)
    {
        try
        {
            return _refreshTokenService.ValidateRefreshTokenAsync(token, username).Result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error validando refresh token para usuario {Username}", username);
            return false;
        }
    }

    public void RevokeRefreshToken(string token)
    {
        try
        {
            _refreshTokenService.RevokeRefreshTokenAsync(token).Wait();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revocando refresh token");
        }
    }

    public void RevokeAllUserRefreshTokens(string username)
    {
        try
        {
            _refreshTokenService.RevokeAllUserRefreshTokensAsync(username).Wait();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revocando todos los refresh tokens del usuario {Username}", username);
        }
    }

    public bool IsTokenExpired(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            return jsonToken.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}

