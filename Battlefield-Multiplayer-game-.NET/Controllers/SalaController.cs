namespace Battlefield_Multiplayer_game_.NET.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Services;

[ApiController]
[Route("sala")]
[Authorize]
public class SalaController : ControllerBase
{
    private readonly ISalaService _salas;

    public SalaController(ISalaService sala)
    {
        _salas = sala;
    }

    [HttpPost("create")]
    public IActionResult CreateRoom()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized("Debes iniciar sesión para acceder a las salas");

        var sala = _salas.CreateSala(username);
        return Ok(new {
            sala.Codigo,
            sala.Estado,
            sala.Jugadores,
            Mensaje = "Sala creada y te has unido automáticamente"
        });
    }

    [HttpPost("join")]
    public IActionResult JoinRoom([FromBody] JoinDto dto)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized("Debes iniciar sesión para acceder a las salas");

        var sala = _salas.GetSala(dto.Codigo);
        if (sala == null)
            return NotFound("La sala no existe");

        if (sala.Jugadores.Contains(username))
            return BadRequest("Ya eres miembro de la sala");

        var salaJoined = _salas.JoinSala(dto.Codigo, username);
        if (salaJoined == null)
            return BadRequest("La sala está llena y no puedes unirte");

        return Ok(new {
            salaJoined.Codigo,
            salaJoined.Estado,
            salaJoined.Jugadores,
            Mensaje = "Haz ingresado a la sala correctamente"
        });
    }

    [HttpPost("leave")]
    public IActionResult LeaveRoom([FromBody] JoinDto dto)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized("Debes iniciar sesión para acceder a las salas");

        var sala = _salas.GetSala(dto.Codigo);
        if (sala == null)
            return NotFound("La sala no existe");

        if (!sala.Jugadores.Contains(username))
            return BadRequest("No eres miembro de la sala");

        sala.Jugadores.Remove(username);
        if (sala.Jugadores.Count == 0)
        {
            // Elimina la sala si está vacía
            _salas.EliminarSala(dto.Codigo);
            return Ok(new { Mensaje = "Has salido de la sala y la sala ha sido eliminada automáticamente" });
        }
        return Ok(new { Mensaje = "Has salido de la sala correctamente" });
    }

    [HttpGet("{codigo}")]
    public IActionResult GetRoom(string codigo)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized("Debes iniciar sesión para acceder a las salas");

        var sala = _salas.GetSala(codigo);
        if (sala == null)
            return NotFound("La sala no existe");

        return Ok(new {
            sala.Codigo,
            sala.Estado,
            sala.Jugadores
        });
    }

    [HttpGet]
    public IActionResult GetAllRooms()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized("Debes iniciar sesión para acceder a las salas");

        var rooms = _salas.GelALLSalas();
        return Ok(rooms.Select(r => new {
            r.Codigo,
            r.Estado,
            r.Jugadores
        }));
    }

    public class JoinDto
    {
        public string Codigo { get; set; } = string.Empty;
    }
}





