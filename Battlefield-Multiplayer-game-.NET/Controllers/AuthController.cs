using Battlefield_Multiplayer_game_.NET.Models;
using Battlefield_Multiplayer_game_.NET.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Battlefield_Multiplayer_game_.NET.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ITokenServices _tokens;
    private readonly ICookieService _cookies;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserService users, 
        ITokenServices tokens, 
        ICookieService cookies,
        ILogger<AuthController> logger)
    {
        _users = users;
        _tokens = tokens;
        _cookies = cookies;
        _logger = logger;
    }

    private string? GetUsernameFromCookie()
    {
        try
        {
            var accessToken = _cookies.GetCookie("accessToken");
            if (string.IsNullOrWhiteSpace(accessToken)) return null;

            var principal = _tokens.GetPrincipalFromToken(accessToken);
            return principal?.Identity?.Name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error obteniendo username desde cookie");
            return null;
        }
    }

    private string? GetClientInfo()
    {
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        return $"{userAgent}|{ipAddress}";
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = _users.Register(request.Username, request.Password);
            if (result == null) 
            {
                _logger.LogWarning("Intento de registro fallido - Usuario ya existe: {Username}", request.Username);
                return BadRequest(new { message = "Usuario ya existe" });
            }

            _logger.LogInformation("Usuario registrado exitosamente: {Username}", request.Username);
            return Ok(new { message = "Usuario registrado correctamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante registro de usuario {Username}", request.Username);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = _users.Login(request.Username, request.Password);
            if (result == null) 
            {
                _logger.LogWarning("Intento de login fallido para usuario: {Username}", request.Username);
                return Unauthorized(new { message = "Credenciales inválidas" });
            }

            var clientInfo = GetClientInfo();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var accessToken = _tokens.GenerateAccessToken(request.Username, "player");
            var refreshToken = _tokens.GenerateRefreshToken(request.Username, clientInfo, ipAddress);

            _cookies.SetAuthCookies(accessToken, refreshToken.Token);

            _logger.LogInformation("Login exitoso para usuario: {Username}", request.Username);
            return Ok(new { 
                message = "Login exitoso",
                user = new { username = request.Username }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante login de usuario {Username}", request.Username);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        try
        {
            var expiredAccessToken = _cookies.GetCookie("accessToken");
            var cookieRefreshToken = _cookies.GetCookie("refreshToken");

            if (string.IsNullOrWhiteSpace(expiredAccessToken) || string.IsNullOrWhiteSpace(cookieRefreshToken))
            {
                _logger.LogWarning("Intento de refresh sin tokens válidos");
                return Unauthorized(new { message = "Tokens no encontrados" });
            }

            var principal = _tokens.GetPrincipalFromExpiredToken(expiredAccessToken);
            var username = principal?.Identity?.Name;

            if (username == null || !_tokens.ValidateRefreshToken(cookieRefreshToken, username))
            {
                _logger.LogWarning("Refresh token inválido para usuario: {Username}", username);
                return Unauthorized(new { message = "Refresh token inválido" });
            }

            _tokens.RevokeRefreshToken(cookieRefreshToken);

            var clientInfo = GetClientInfo();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var newAccessToken = _tokens.GenerateAccessToken(username, "player");
            var newRefreshToken = _tokens.GenerateRefreshToken(username, clientInfo, ipAddress);

            _cookies.SetAuthCookies(newAccessToken, newRefreshToken.Token);

            _logger.LogInformation("Tokens renovados exitosamente para usuario: {Username}", username);
            return Ok(new { message = "Tokens renovados correctamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante refresh de tokens");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var username = GetUsernameFromCookie();
            var refreshToken = _cookies.GetCookie("refreshToken");

            if (!string.IsNullOrEmpty(refreshToken))
            {
                _tokens.RevokeRefreshToken(refreshToken);
            }

            _cookies.ClearAuthCookies();

            if (!string.IsNullOrEmpty(username))
            {
                _logger.LogInformation("Logout exitoso para usuario: {Username}", username);
            }

            return Ok(new { message = "Sesión cerrada correctamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante logout");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        try
        {
            var username = GetUsernameFromCookie();
            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { message = "No has iniciado sesión" });

            _tokens.RevokeAllUserRefreshTokens(username);
            _cookies.ClearAuthCookies();

            _logger.LogInformation("Logout de todas las sesiones para usuario: {Username}", username);
            return Ok(new { message = "Todas las sesiones han sido cerradas" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante logout de todas las sesiones");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetUser()
    {
        try
        {
            var username = GetUsernameFromCookie();
            if (string.IsNullOrWhiteSpace(username))
                return Unauthorized(new { message = "No has iniciado sesión" });

            return Ok(new { username });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo información del usuario");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    [HttpGet("validate")]
    public async Task<IActionResult> ValidateToken()
    {
        try
        {
            var username = GetUsernameFromCookie();
            if (string.IsNullOrWhiteSpace(username))
                return Unauthorized(new { valid = false, message = "Token inválido" });

            return Ok(new { valid = true, username });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validando token");
            return Ok(new { valid = false, message = "Token inválido" });
        }
    }
}

public class RegisterRequest
{
    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    public string Password { get; set; } = string.Empty;
}
