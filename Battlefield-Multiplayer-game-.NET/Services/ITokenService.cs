using System.Security.Claims;

namespace Battlefield_Multiplayer_game_.NET.Services
{
    public interface ITokenServices
    {
        string GenerateAccessToken(string username);


        string GenerateRefreshToken();


        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    }

}


