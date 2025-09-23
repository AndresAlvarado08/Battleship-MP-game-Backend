using Microsoft.AspNetCore.SignalR;
using Battlefield_Multiplayer_game_.NET.Services;
using Battlefield_Multiplayer_game_.NET.Models;

namespace Battlefield_Multiplayer_game_.NET.Hubs;

public class SignalR : Hub
{
    private readonly ITokenServices _tokens;
    private readonly ILogger<SignalR> _logger;

    public SignalR(ITokenServices tokens, ILogger<SignalR> logger)
    {
        _tokens = tokens;
        _logger = logger;
    }

    private string? GetUsernameFromContext()
    {
        try
        {
            var accessToken = Context.GetHttpContext()?.Request.Cookies["accessToken"];
            if (string.IsNullOrWhiteSpace(accessToken)) return null;

            var principal = _tokens.GetPrincipalFromToken(accessToken);
            return principal?.Identity?.Name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error obteniendo username en SignalR");
            return null;
        }
    }

    public override async Task OnConnectedAsync()
    {
        var username = GetUsernameFromContext();
        if (!string.IsNullOrEmpty(username))
        {
            _logger.LogInformation("Usuario {Username} conectado a SignalR", username);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var username = GetUsernameFromContext();
        if (!string.IsNullOrEmpty(username))
        {
            _logger.LogInformation("Usuario {Username} desconectado de SignalR", username);
        }
        await base.OnDisconnectedAsync(exception);
    }

    // 🏠 Métodos para Salas
    public async Task JoinSalaGroup(string codigoSala)
    {
        var username = GetUsernameFromContext();
        if (string.IsNullOrEmpty(username)) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"sala_{codigoSala}");
        _logger.LogInformation("Usuario {Username} se unió al grupo de sala {CodigoSala}", username, codigoSala);
    }

    public async Task LeaveSalaGroup(string codigoSala)
    {
        var username = GetUsernameFromContext();
        if (string.IsNullOrEmpty(username)) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sala_{codigoSala}");
        _logger.LogInformation("Usuario {Username} salió del grupo de sala {CodigoSala}", username, codigoSala);
    }

    // 🎮 Métodos para Juego Warship
    public async Task JoinGameGroup(string codigoSala)
    {
        var username = GetUsernameFromContext();
        if (string.IsNullOrEmpty(username)) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"game_{codigoSala}");
        _logger.LogInformation("Usuario {Username} se unió al grupo de juego {CodigoSala}", username, codigoSala);
    }

    public async Task LeaveGameGroup(string codigoSala)
    {
        var username = GetUsernameFromContext();
        if (string.IsNullOrEmpty(username)) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game_{codigoSala}");
        _logger.LogInformation("Usuario {Username} salió del grupo de juego {CodigoSala}", username, codigoSala);
    }

    // 🎯 Notificaciones de disparo
    public async Task NotifyShot(string codigoSala, int fila, int columna)
    {
        var username = GetUsernameFromContext();
        if (string.IsNullOrEmpty(username)) return;

        var shotNotification = new
        {
            shooter = username,
            fila = fila,
            columna = columna,
            timestamp = DateTime.UtcNow,
            type = "shot_attempt"
        };

        await Clients.Group($"game_{codigoSala}").SendAsync("ReceiveShotAttempt", shotNotification);
    }
}

// 🚀 Clase para enviar notificaciones desde servicios
public interface ISignalRService
{
    Task NotifyRoomUpdate(string codigoSala, object roomData);
    Task NotifyGameStarted(string codigoSala, object gameData);
    Task NotifyGameUpdate(string codigoSala, object gameUpdate);
    Task NotifyPlayerEliminated(string codigoSala, string eliminatedPlayer);
    Task NotifyGameEnded(string codigoSala, string winner);
    Task NotifyTurnChange(string codigoSala, string currentPlayer, string nextPlayer);
    Task NotifyShot(string codigoSala, DisparoResultado shotResult);
}

public class SignalRService : ISignalRService
{
    private readonly IHubContext<SignalR> _hubContext;
    private readonly ILogger<SignalRService> _logger;

    public SignalRService(IHubContext<SignalR> hubContext, ILogger<SignalRService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyRoomUpdate(string codigoSala, object roomData)
    {
        try
        {
            await _hubContext.Clients.Group($"sala_{codigoSala}").SendAsync("RoomUpdated", roomData);
            _logger.LogDebug("Notificación de actualización de sala enviada: {CodigoSala}", codigoSala);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando notificación de actualización de sala");
        }
    }

    public async Task NotifyGameStarted(string codigoSala, object gameData)
    {
        try
        {
            await _hubContext.Clients.Group($"sala_{codigoSala}").SendAsync("GameStarted", gameData);
            _logger.LogInformation("Notificación de juego iniciado enviada: {CodigoSala}", codigoSala);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando notificación de juego iniciado");
        }
    }

    public async Task NotifyGameUpdate(string codigoSala, object gameUpdate)
    {
        try
        {
            await _hubContext.Clients.Group($"game_{codigoSala}").SendAsync("GameUpdated", gameUpdate);
            _logger.LogDebug("Notificación de actualización de juego enviada: {CodigoSala}", codigoSala);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando notificación de actualización de juego");
        }
    }

    public async Task NotifyPlayerEliminated(string codigoSala, string eliminatedPlayer)
    {
        try
        {
            var notification = new
            {
                type = "player_eliminated",
                player = eliminatedPlayer,
                message = $"¡{eliminatedPlayer} ha sido eliminado!",
                timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"game_{codigoSala}").SendAsync("PlayerEliminated", notification);
            _logger.LogInformation("Jugador eliminado notificado: {Player} en sala {CodigoSala}", eliminatedPlayer, codigoSala);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando notificación de jugador eliminado");
        }
    }

    public async Task NotifyGameEnded(string codigoSala, string winner)
    {
        try
        {
            var notification = new
            {
                type = "game_ended",
                winner = winner,
                message = $"🏆 ¡{winner} ha ganado la batalla!",
                timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"game_{codigoSala}").SendAsync("GameEnded", notification);
            _logger.LogInformation("Juego terminado notificado: Ganador {Winner} en sala {CodigoSala}", winner, codigoSala);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando notificación de juego terminado");
        }
    }

    public async Task NotifyTurnChange(string codigoSala, string currentPlayer, string nextPlayer)
    {
        try
        {
            var notification = new
            {
                type = "turn_change",
                currentPlayer = currentPlayer,
                nextPlayer = nextPlayer,
                message = $"Turno de {nextPlayer}",
                timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"game_{codigoSala}").SendAsync("TurnChanged", notification);
            _logger.LogDebug("Cambio de turno notificado: {NextPlayer} en sala {CodigoSala}", nextPlayer, codigoSala);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando notificación de cambio de turno");
        }
    }

    public async Task NotifyShot(string codigoSala, DisparoResultado shotResult)
    {
        try
        {
            var notification = new
            {
                type = "shot_result",
                fila = shotResult.Fila,
                columna = shotResult.Columna,
                acierto = shotResult.Acierto,
                usuarioImpactado = shotResult.UsuarioImpactado,
                usuarioEliminado = shotResult.UsuarioEliminado,
                mensaje = shotResult.Mensaje,
                timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"game_{codigoSala}").SendAsync("ShotResult", notification);
            _logger.LogDebug("Resultado de disparo notificado en sala {CodigoSala}: {Resultado}", codigoSala, shotResult.Mensaje);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando notificación de resultado de disparo");
        }
    }
}
