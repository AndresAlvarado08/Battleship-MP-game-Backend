using System.Security.Claims;
using Battlefield_Multiplayer_game_.NET.Models;

namespace Battlefield_Multiplayer_game_.NET.Services
{
    public interface ITokenServices
    {
        string GenerateAccessToken(string username, string? role = null, Dictionary<string, string>? additionalClaims = null);
        RefreshToken GenerateRefreshToken(string username, string? deviceInfo = null, string? ipAddress = null);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        ClaimsPrincipal? GetPrincipalFromToken(string token);
        bool ValidateRefreshToken(string token, string username);
        void RevokeRefreshToken(string token);
        void RevokeAllUserRefreshTokens(string username);
        bool IsTokenExpired(string token);
    }
}


