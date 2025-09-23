using Battlefield_Multiplayer_game_.NET.Services;
using Battlefield_Multiplayer_game_.NET.Hubs;
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
        private readonly ILogger<SalaController> _logger;
        private readonly ISignalRService _signalRService;

        public SalaController(ISalaService salas, ITokenServices tokens, ILogger<SalaController> logger, ISignalRService signalRService)
        {
            _salas = salas;
            _tokens = tokens;
            _logger = logger;
            _signalRService = signalRService;
        }

        private string? GetUsernameFromCookie()
        {
            try
            {
                var accessToken = Request.Cookies["accessToken"];
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

        [HttpPost("crear")]
        public async Task<IActionResult> CrearSala()
        {
            try
            {
                var username = GetUsernameFromCookie();
                if (string.IsNullOrWhiteSpace(username))
                    return Unauthorized(new { message = "Debes iniciar sesión para acceder a las salas" });

                var sala = _salas.CreateSala(username);
                if (sala == null)
                    return BadRequest(new { message = "Ya estás en una sala, no puedes crear otra" });

                _logger.LogInformation("Sala creada exitosamente: {Codigo} por usuario: {Username}", sala.Codigo, username);
                
                var roomData = new
                {
                    codigo = sala.Codigo,
                    estado = sala.Estado,
                    jugadores = sala.Jugadores ?? new List<string>(),
                    creador = sala.Creador,
                    message = "Sala creada y te has unido automáticamente"
                };

                // 🚀 Notificar creación de sala via SignalR
                _ = Task.Run(async () => await _signalRService.NotifyRoomUpdate(sala.Codigo, roomData));

                return Ok(roomData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear sala");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("unirse/{codigo}")]
        public async Task<IActionResult> Unirse(string codigo)
        {
            try
            {
                var username = GetUsernameFromCookie();
                if (string.IsNullOrWhiteSpace(username))
                    return Unauthorized(new { message = "Debes iniciar sesión para acceder a las salas" });

                var sala = _salas.GetSala(codigo);
                if (sala == null) 
                    return NotFound(new { message = "La sala no existe" });

                var salaJoined = _salas.JoinSala(codigo, username);
                if (salaJoined == null)
                {
                    if (_salas.GetSala(codigo)?.Jugadores.Contains(username) == true)
                        return BadRequest(new { message = "Ya eres miembro de la sala" });

                    return BadRequest(new { message = "Ya estás en otra sala o la sala está llena" });
                }

                _logger.LogInformation("Usuario {Username} se unió a la sala {Codigo}", username, codigo);

                var roomData = new
                {
                    codigo = salaJoined.Codigo,
                    estado = salaJoined.Estado,
                    jugadores = salaJoined.Jugadores,
                    creador = salaJoined.Creador,
                    message = "Te has unido a la sala correctamente"
                };

                // 🚀 Notificar actualización de sala via SignalR
                _ = Task.Run(async () => await _signalRService.NotifyRoomUpdate(codigo, roomData));

                return Ok(roomData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al unirse a la sala {Codigo}", codigo);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("salir/{codigo}")]
        public async Task<IActionResult> Salir(string codigo)
        {
            try
            {
                var username = GetUsernameFromCookie();
                if (string.IsNullOrWhiteSpace(username))
                    return Unauthorized(new { message = "Debes iniciar sesión para acceder a las salas" });

                var sala = _salas.GetSala(codigo);
                if (sala == null) 
                    return NotFound(new { message = "La sala no existe" });

                if (!sala.Jugadores.Contains(username))
                    return BadRequest(new { message = "No eres miembro de la sala" });

                _salas.SalirDeSala(codigo, username);
                
                _logger.LogInformation("Usuario {Username} salió de la sala {Codigo}", username, codigo);

                // Obtener estado actualizado de la sala (puede haber sido eliminada)
                var salaActualizada = _salas.GetSala(codigo);
                if (salaActualizada != null)
                {
                    var roomData = new
                    {
                        codigo = salaActualizada.Codigo,
                        estado = salaActualizada.Estado,
                        jugadores = salaActualizada.Jugadores,
                        creador = salaActualizada.Creador,
                        message = $"{username} ha salido de la sala"
                    };

                    // 🚀 Notificar actualización de sala via SignalR
                    _ = Task.Run(async () => await _signalRService.NotifyRoomUpdate(codigo, roomData));
                }

                return Ok(new { message = "Has salido de la sala correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al salir de la sala {Codigo}", codigo);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("{codigo}")]
        public IActionResult ObtenerSala(string codigo)
        {
            try
            {
                var username = GetUsernameFromCookie();
                if (string.IsNullOrWhiteSpace(username))
                    return Unauthorized(new { message = "Debes iniciar sesión para acceder a las salas" });

                var sala = _salas.GetSala(codigo);
                if (sala == null) 
                    return NotFound(new { message = "La sala no existe" });

                return Ok(new
                {
                    codigo = sala.Codigo,
                    estado = sala.Estado,
                    jugadores = sala.Jugadores,
                    creador = sala.Creador
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener sala {Codigo}", codigo);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet]
        public IActionResult ObtenerTodas()
        {
            try
            {
                var username = GetUsernameFromCookie();
                if (string.IsNullOrWhiteSpace(username))
                    return Unauthorized(new { message = "Debes iniciar sesión para acceder a las salas" });

                var rooms = _salas.GelALLSalas();
                
                var result = rooms.Select(r => new
                {
                    codigo = r.Codigo,
                    estado = r.Estado,
                    jugadores = r.Jugadores,
                    creador = r.Creador
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las salas");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("user")]
        public IActionResult GetUser()
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
                _logger.LogError(ex, "Error al obtener información del usuario");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}
