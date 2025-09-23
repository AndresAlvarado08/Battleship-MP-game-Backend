using Battlefield_Multiplayer_game_.NET.Models;
using Battlefield_Multiplayer_game_.NET.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ITokenServices _tokens;

    private static readonly Dictionary<string, string> _refreshTokens = new();
    public AuthController(IUserService users, ITokenServices tokens)
    {
        _users = users;
        _tokens = tokens;
    }
    private string? GetUsernameFromCookie()
    {
        var accessToken = Request.Cookies["accessToken"];
        if (string.IsNullOrWhiteSpace(accessToken)) return null;

        var principal = _tokens.GetPrincipalFromExpiredToken(accessToken);
        return principal?.Identity?.Name;
    }


    [HttpPost("register")]
    public IActionResult Register([FromBody] Usuario user)
    {
        if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
            return BadRequest("El nombre de usuario o la contraseña no pueden estar vacios");

        var result = _users.Register(user.Username, user.Password);
        if (result == null) return BadRequest("Usuario ya existe");

        return Ok(new { message = "Usuario registrado correctamente" });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] Usuario user)
    {
        var result = _users.Login(user.Username, user.Password);
        if (result == null) return Unauthorized();

        var accessToken = _tokens.GenerateAccessToken(user.Username);
        var refreshToken = _tokens.GenerateRefreshToken();

        _refreshTokens[user.Username] = refreshToken;


        Response.Cookies.Append("accessToken", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Strict
        });

        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Strict
        });

        return Ok(new { message = "Login exitoso" });
    }

    [HttpPost("refresh")]
    public IActionResult Refresh()
    {
        var expiredAccessToken = Request.Cookies["accessToken"];
        var cookieRefreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrWhiteSpace(expiredAccessToken) || string.IsNullOrWhiteSpace(cookieRefreshToken))
            return Unauthorized();

        var principal = _tokens.GetPrincipalFromExpiredToken(expiredAccessToken);
        var username = principal?.Identity?.Name;

        if (username == null || !_refreshTokens.TryGetValue(username, out var savedRefreshToken))
            return Unauthorized();

        if (cookieRefreshToken != savedRefreshToken)
            return Unauthorized();

        var newAccessToken = _tokens.GenerateAccessToken(username);
        var newRefreshToken = _tokens.GenerateRefreshToken();

        _refreshTokens[username] = newRefreshToken;

        Response.Cookies.Append("accessToken", newAccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Strict
        });

        Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Strict
        });

        return Ok(new { message = "Tokens renovados correctamente" });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");
        return Ok("Sesion cerrada, cookies eliminadas");
    }

    [HttpGet("user")]
    public IActionResult GetUser()
    {
        var username = GetUsernameFromCookie();
        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized("No has iniciado sesion");

        return Ok(new { username });
    }

}
