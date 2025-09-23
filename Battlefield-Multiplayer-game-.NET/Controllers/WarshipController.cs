using Battlefield_Multiplayer_game_.NET.Models;
using Battlefield_Multiplayer_game_.NET.Services;
using Microsoft.AspNetCore.Mvc;

namespace Battlefield_Multiplayer_game_.NET.Controllers;

[ApiController]
[Route("warship")]
public class WarshipController : ControllerBase
{
    private readonly IWarshipService _warshipService;
    private readonly ISalaService _salaService;
    private readonly ITokenServices _tokens;
    private readonly ILogger<WarshipController> _logger;

    public WarshipController(
        IWarshipService warshipService,
        ISalaService salaService, 
        ITokenServices tokens,
        ILogger<WarshipController> logger)
    {
        _warshipService = warshipService;
        _salaService = salaService;
        _tokens = tokens;
        _logger = logger;
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

    [HttpPost("iniciar/{codigoSala}")]
    public IActionResult IniciarJuego(string codigoSala)
    {
        try
        {
            var username = GetUsernameFromCookie();
            if (string.IsNullOrWhiteSpace(username))
                return Unauthorized(new { message = "Debes iniciar sesión" });

            // Verificar que la sala existe y está en curso
            var sala = _salaService.GetSala(codigoSala);
            if (sala == null)
                return NotFound(new { message = "La sala no existe" });

            if (sala.Estado != "En curso")
                return BadRequest(new { message = "La sala debe estar en curso para iniciar el juego" });

            if (sala.Jugadores.Count != 6)
                return BadRequest(new { message = "Se necesitan exactamente 6 jugadores para iniciar" });

            // Verificar que el usuario está en la sala
            if (!sala.Jugadores.Contains(username))
                return StatusCode(403, new { message = "No eres miembro de esta sala" });

            // Iniciar el juego
            var juego = _warshipService.IniciarJuego(codigoSala, sala.Jugadores);
            if (juego == null)
                return BadRequest(new { message = "Error al iniciar el juego" });

            _logger.LogInformation("Juego Warship iniciado en sala {CodigoSala} por {Username}", codigoSala, username);

            return Ok(new
            {
                message = "¡Juego Warship iniciado!",
                codigoSala = juego.CodigoSala,
                estado = juego.Estado.ToString(),
                usuarios = juego.Usuarios.Select(u => new
                {
                    username = u.Username,
                    puntos = u.Puntos,
                    sigueEnJuego = u.SigueEnJuego,
                    casillasBarco = u.Barco.Count
                }),
                turnoActual = juego.UsuarioTurnoActual.Username,
                tablero = _warshipService.ObtenerEstadoTablero(codigoSala)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al iniciar juego en sala {CodigoSala}", codigoSala);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    [HttpPost("disparar/{codigoSala}")]
    public IActionResult Disparar(string codigoSala, [FromBody] DisparoRequest request)
    {
        try
        {
            var username = GetUsernameFromCookie();
            if (string.IsNullOrWhiteSpace(username))
                return Unauthorized(new { message = "Debes iniciar sesión" });

            var resultado = _warshipService.Disparar(codigoSala, username, request.Fila, request.Columna);
            if (resultado == null)
                return BadRequest(new { message = "Disparo inválido o no es tu turno" });

            var juego = _warshipService.ObtenerJuego(codigoSala);
            if (juego == null)
                return NotFound(new { message = "Juego no encontrado" });

            _logger.LogInformation("Disparo en {CodigoSala}: {Username} -> ({Fila},{Columna}) = {Resultado}", 
                codigoSala, username, request.Fila, request.Columna, resultado.Acierto ? "Acierto" : "Agua");

            return Ok(new
            {
                disparo = new
                {
                    fila = resultado.Fila,
                    columna = resultado.Columna,
                    acierto = resultado.Acierto,
                    usuarioImpactado = resultado.UsuarioImpactado,
                    usuarioEliminado = resultado.UsuarioEliminado,
                    mensaje = resultado.Mensaje
                },
                juegoTerminado = juego.JuegoTerminado,
                ganador = juego.Ganador?.Username,
                proximoTurno = juego.JuegoTerminado ? null : juego.UsuarioTurnoActual.Username,
                tablero = _warshipService.ObtenerEstadoTablero(codigoSala),
                estadisticas = _warshipService.ObtenerEstadisticas(codigoSala)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar disparo en sala {CodigoSala}", codigoSala);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    [HttpGet("estado/{codigoSala}")]
    public IActionResult ObtenerEstado(string codigoSala)
    {
        try
        {
            var username = GetUsernameFromCookie();
            if (string.IsNullOrWhiteSpace(username))
                return Unauthorized(new { message = "Debes iniciar sesión" });

            var juego = _warshipService.ObtenerJuego(codigoSala);
            if (juego == null)
                return NotFound(new { message = "Juego no encontrado" });

            // Verificar que el usuario está en el juego
            if (!juego.Usuarios.Any(u => u.Username == username))
                return StatusCode(403, new { message = "No eres parte de este juego" });

            return Ok(new
            {
                codigoSala = juego.CodigoSala,
                estado = juego.Estado.ToString(),
                juegoTerminado = juego.JuegoTerminado,
                ganador = juego.Ganador?.Username,
                turnoActual = juego.JuegoTerminado ? null : juego.UsuarioTurnoActual.Username,
                esTuTurno = !juego.JuegoTerminado && juego.UsuarioTurnoActual.Username == username,
                usuarios = juego.Usuarios.Select(u => new
                {
                    username = u.Username,
                    puntos = u.Puntos,
                    sigueEnJuego = u.SigueEnJuego,
                    casillasBarco = u.Barco.Count
                }),
                tablero = _warshipService.ObtenerEstadoTablero(codigoSala),
                estadisticas = _warshipService.ObtenerEstadisticas(codigoSala)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estado del juego en sala {CodigoSala}", codigoSala);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    [HttpGet("tablero/{codigoSala}")]
    public IActionResult ObtenerTablero(string codigoSala)
    {
        try
        {
            var username = GetUsernameFromCookie();
            if (string.IsNullOrWhiteSpace(username))
                return Unauthorized(new { message = "Debes iniciar sesión" });

            var juego = _warshipService.ObtenerJuego(codigoSala);
            if (juego == null)
                return NotFound(new { message = "Juego no encontrado" });

            if (!juego.Usuarios.Any(u => u.Username == username))
                return StatusCode(403, new { message = "No eres parte de este juego" });

            return Ok(new
            {
                tablero = _warshipService.ObtenerEstadoTablero(codigoSala),
                estadisticas = _warshipService.ObtenerEstadisticas(codigoSala)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tablero en sala {CodigoSala}", codigoSala);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}

public class DisparoRequest
{
    public int Fila { get; set; }
    public int Columna { get; set; }
}