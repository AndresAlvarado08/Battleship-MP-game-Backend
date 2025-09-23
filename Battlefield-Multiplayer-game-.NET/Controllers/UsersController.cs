namespace Battlefield_Multiplayer_game_.NET.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Battlefield_Multiplayer_game_.NET.Services;

[ApiController]
[Route("users")]
[Authorize] // Opcional: quítalo si quieres que sea público
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    /// <summary>
    /// Obtiene la lista de usuarios registrados (solo nombres de usuario).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public IActionResult GetAllUsers()
    {
        // Importante: no exponer contraseñas (ni siquiera hash).
        // Mapear a un objeto anónimo con solo el username.
        var result = _users.GetAll().Select(u => new { username = u.Username });
        return Ok(result);
    }
}
