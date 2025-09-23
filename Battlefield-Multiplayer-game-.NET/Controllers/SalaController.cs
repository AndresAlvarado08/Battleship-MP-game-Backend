using Battlefield_Multiplayer_game_.NET.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Battlefield_Multiplayer_game_.NET.Controllers
{
    [ApiController]
    [Route("sala")]
    public class SalaController : ControllerBase
    {
        private readonly ISalaService _salas;
        private readonly ITokenServices _tokens;

        public SalaController(ISalaService salas, ITokenServices tokens)
        {
            _salas = salas;
            _tokens = tokens;
        }
        private string? GetUsernameFromCookie()
        {
            var accessToken = Request.Cookies["accessToken"];
            if (string.IsNullOrWhiteSpace(accessToken)) return null;

            var principal = _tokens.GetPrincipalFromExpiredToken(accessToken);
            return principal?.Identity?.Name;
        }

        [HttpPost("crear")]
        public IActionResult CrearSala()
        {
            var username = GetUsernameFromCookie();
            if (string.IsNullOrWhiteSpace(username))
                return Unauthorized("Debes iniciar sesion para acceder a las salas");

            var sala = _salas.CreateSala(username);
            if (sala == null)
                return BadRequest("Ya estas en una sala no puedes crear otra.");

            return Ok(new
            {
                sala.Codigo,
                sala.Estado,
                Jugadores = sala.Jugadores ?? new List<string>(),
                Mensaje = "Sala creada y te has unido automaticamente"
            });
        }

        [HttpPost("unirse/{codigo}")]
        public IActionResult Unirse(string codigo)
        {
            var username = GetUsernameFromCookie();
            if (string.IsNullOrWhiteSpace(username))
                return Unauthorized("Debes iniciar sesion para acceder a las salas");

            var sala = _salas.GetSala(codigo);
            if (sala == null) return NotFound("La sala no existe");

            var salaJoined = _salas.JoinSala(codigo, username);
            if (salaJoined == null)
            {
                if (_salas.GetSala(codigo)?.Jugadores.Contains(username) == true)
                    return BadRequest("Ya eres miembro de la sala");

                return BadRequest("Ya estas en otra sala o la sala esta llena");
            }


            return Ok(new
            {
                salaJoined.Codigo,
                salaJoined.Estado,
                salaJoined.Jugadores,
                Mensaje = "Haz ingresado a la sala correctamente"
            });
        }

        [HttpPost("salir/{codigo}")]
        public IActionResult Salir(string codigo)
        {
            var username = GetUsernameFromCookie();
            if (string.IsNullOrWhiteSpace(username))
                return Unauthorized("Debes iniciar sesion para acceder a las salas");

            var sala = _salas.GetSala(codigo);
            if (sala == null) return NotFound("La sala no existe");

            if (!sala.Jugadores.Contains(username))
                return BadRequest("No eres miembro de la sala");

            _salas.SalirDeSala(codigo, username);
            return Ok(new { Mensaje = "Has salido de la sala correctamente" });
        }

        [HttpGet("{codigo}")]
        public IActionResult ObtenerSala(string codigo)
        {
            var username = GetUsernameFromCookie();
            if (string.IsNullOrWhiteSpace(username))
                return Unauthorized("Debes iniciar sesion para acceder a las salas");

            var sala = _salas.GetSala(codigo);
            if (sala == null) return NotFound("La sala no existe");

            return Ok(new
            {
                sala.Codigo,
                sala.Estado,
                sala.Jugadores
            });
        }

        [HttpGet]
        public IActionResult ObtenerTodas()
        {
            var username = GetUsernameFromCookie();
            if (string.IsNullOrWhiteSpace(username))
                return Unauthorized("Debes iniciar sesion para acceder a las salas");

            var rooms = _salas.GelALLSalas();
            return Ok(rooms.Select(r => new
            {
                r.Codigo,
                r.Estado,
                r.Jugadores
            }));
        }

        [HttpGet("user")]
        public IActionResult GetUser()
        {
            var username = GetUsernameFromCookie();
            if (string.IsNullOrWhiteSpace(username))
                return Unauthorized("No has iniciado sesión");

            return Ok(new { username });
        }

    }
}
