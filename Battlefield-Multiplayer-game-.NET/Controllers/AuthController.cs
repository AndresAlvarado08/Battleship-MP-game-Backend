using Battlefield_Multiplayer_game_.NET.Models;
using Battlefield_Multiplayer_game_.NET.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;

    public AuthController(IUserService users)
    {
        _users = users;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] Usuario user)
    {
        if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
            return BadRequest("El nombre de usuario o la contraseña no pueden estar vacíos");

        var result = _users.Register(user.Username, user.Password);
        if (result == null)
            return BadRequest("Usuario ya existe");

        return Ok(new { message = "Usuario registrado correctamente" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Usuario user)
    {
        if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
            return BadRequest("El nombre de usuario o la contraseña no pueden estar vacíos");

        var result = _users.Login(user.Username, user.Password);
        if (result == null)
            return Unauthorized();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Ok(new { message = "Inicio de sesión exitosa" }); 
        
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok("Sesión cerrada, cookie eliminada");
    }
}
