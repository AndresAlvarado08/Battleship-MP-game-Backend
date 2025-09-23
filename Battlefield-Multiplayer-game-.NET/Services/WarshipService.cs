using Battlefield_Multiplayer_game_.NET.Models;
using Battlefield_Multiplayer_game_.NET.Hubs;

namespace Battlefield_Multiplayer_game_.NET.Services;

public interface IWarshipService
{
    JuegoWarship? IniciarJuego(string codigoSala, List<string> usernames);
    JuegoWarship? ObtenerJuego(string codigoSala);
    DisparoResultado? Disparar(string codigoSala, string username, int fila, int columna);
    string ObtenerEstadoTablero(string codigoSala);
    List<string> ObtenerEstadisticas(string codigoSala);
}

public class WarshipService : IWarshipService
{
    private readonly Dictionary<string, JuegoWarship> _juegos = new();
    private readonly Random _random = new();
    private readonly ILogger<WarshipService> _logger;
    private readonly ISignalRService _signalRService;

    public WarshipService(ILogger<WarshipService> logger, ISignalRService signalRService)
    {
        _logger = logger;
        _signalRService = signalRService;
    }

    public JuegoWarship? IniciarJuego(string codigoSala, List<string> usernames)
    {
        try
        {
            if (usernames.Count != 6)
            {
                _logger.LogWarning("Intento de iniciar juego con {Count} usuarios en lugar de 6", usernames.Count);
                return null;
            }

            // Crear usuarios para el juego
            var usuarios = usernames.Select(name => new Usuario 
            { 
                Username = name,
                Barco = new List<(int, int)>(),
                Puntos = 0
            }).ToList();

            var juego = new JuegoWarship
            {
                CodigoSala = codigoSala,
                Usuarios = usuarios,
                Tablero = new char[10, 10],
                TurnoActual = 0,
                Estado = EstadoJuego.Preparando,
                IniciadoEn = DateTime.UtcNow
            };

            InicializarTablero(juego);
            ColocarBarcos(juego);
            
            juego.Estado = EstadoJuego.EnCurso;
            _juegos[codigoSala] = juego;

            // ?? Notificar inicio del juego via SignalR
            var gameData = new
            {
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
                tablero = ObtenerEstadoTablero(codigoSala)
            };

            _ = Task.Run(async () => await _signalRService.NotifyGameStarted(codigoSala, gameData));

            _logger.LogInformation("Juego Warship iniciado en sala {CodigoSala} con 6 usuarios", codigoSala);
            return juego;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al iniciar juego en sala {CodigoSala}", codigoSala);
            return null;
        }
    }

    private void InicializarTablero(JuegoWarship juego)
    {
        // Llenar tablero con agua
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                juego.Tablero[i, j] = '~';
            }
        }
    }

    private void ColocarBarcos(JuegoWarship juego)
    {
        var posicionesOcupadas = new HashSet<(int, int)>();

        foreach (var usuario in juego.Usuarios)
        {
            var barcoColocado = false;
            var intentos = 0;

            while (!barcoColocado && intentos < 100)
            {
                var fila = _random.Next(10);
                var columna = _random.Next(10);
                var esHorizontal = _random.Next(2) == 0;

                var posicionesBarco = GenerarPosicionesBarco(fila, columna, esHorizontal);

                // Verificar que todas las posiciones estén libres y dentro del tablero
                if (posicionesBarco.All(pos => 
                    pos.fila >= 0 && pos.fila < 10 && 
                    pos.columna >= 0 && pos.columna < 10 && 
                    !posicionesOcupadas.Contains(pos)))
                {
                    usuario.Barco = posicionesBarco;
                    foreach (var pos in posicionesBarco)
                    {
                        posicionesOcupadas.Add(pos);
                    }
                    barcoColocado = true;
                }
                intentos++;
            }

            if (!barcoColocado)
            {
                // Fallback: colocar en cualquier posición disponible
                ColocarBarcoFallback(usuario, posicionesOcupadas);
            }
        }
    }

    private List<(int fila, int columna)> GenerarPosicionesBarco(int fila, int columna, bool esHorizontal)
    {
        var posiciones = new List<(int, int)>();
        
        for (int i = 0; i < 3; i++)
        {
            if (esHorizontal)
            {
                posiciones.Add((fila, columna + i));
            }
            else
            {
                posiciones.Add((fila + i, columna));
            }
        }
        
        return posiciones;
    }

    private void ColocarBarcoFallback(Usuario usuario, HashSet<(int, int)> posicionesOcupadas)
    {
        // Buscar 3 posiciones consecutivas disponibles
        for (int fila = 0; fila < 10; fila++)
        {
            for (int columna = 0; columna < 8; columna++)
            {
                var posiciones = new List<(int, int)>
                {
                    (fila, columna),
                    (fila, columna + 1),
                    (fila, columna + 2)
                };

                if (posiciones.All(pos => !posicionesOcupadas.Contains(pos)))
                {
                    usuario.Barco = posiciones;
                    foreach (var pos in posiciones)
                    {
                        posicionesOcupadas.Add(pos);
                    }
                    return;
                }
            }
        }
    }

    public JuegoWarship? ObtenerJuego(string codigoSala)
    {
        return _juegos.TryGetValue(codigoSala, out var juego) ? juego : null;
    }

    public DisparoResultado? Disparar(string codigoSala, string username, int fila, int columna)
    {
        try
        {
            var juego = ObtenerJuego(codigoSala);
            if (juego == null || juego.Estado != EstadoJuego.EnCurso)
                return null;

            // Verificar que sea el turno del usuario
            if (juego.UsuarioTurnoActual.Username != username)
                return null;

            // Verificar coordenadas válidas
            if (fila < 0 || fila >= 10 || columna < 0 || columna >= 10)
                return null;

            // Verificar que la casilla no haya sido disparada antes
            if (juego.Tablero[fila, columna] == 'X' || juego.Tablero[fila, columna] == 'O')
                return null;

            var resultado = new DisparoResultado
            {
                Fila = fila,
                Columna = columna
            };

            // Buscar si hay un barco en esa posición
            var usuarioImpactado = juego.Usuarios.FirstOrDefault(u => 
                u.SigueEnJuego && u.Barco.Contains((fila, columna)));

            string turnoAnterior = juego.UsuarioTurnoActual.Username;

            if (usuarioImpactado != null)
            {
                // ¡Acierto!
                juego.Tablero[fila, columna] = 'X';
                usuarioImpactado.Barco.Remove((fila, columna));
                juego.UsuarioTurnoActual.Puntos++;

                resultado.Acierto = true;
                resultado.UsuarioImpactado = usuarioImpactado.Username;
                resultado.UsuarioEliminado = !usuarioImpactado.SigueEnJuego;
                resultado.Mensaje = resultado.UsuarioEliminado 
                    ? $"¡{usuarioImpactado.Username} ha sido eliminado!"
                    : $"¡Impacto en el barco de {usuarioImpactado.Username}!";

                // ?? Notificar eliminación via SignalR
                if (resultado.UsuarioEliminado)
                {
                    _ = Task.Run(async () => await _signalRService.NotifyPlayerEliminated(codigoSala, usuarioImpactado.Username));
                }
            }
            else
            {
                // Agua
                juego.Tablero[fila, columna] = 'O';
                resultado.Acierto = false;
                resultado.Mensaje = "¡Agua!";
            }

            // Agregar al historial
            juego.HistorialTurnos.Add($"{username}: ({fila},{columna}) - {resultado.Mensaje}");

            // ?? Notificar resultado del disparo via SignalR
            _ = Task.Run(async () => await _signalRService.NotifyShot(codigoSala, resultado));

            // Verificar fin del juego
            if (juego.JuegoTerminado)
            {
                juego.Estado = EstadoJuego.Finalizado;
                juego.Ganador = juego.UsuariosActivos.FirstOrDefault();
                
                // ?? Notificar fin del juego via SignalR
                if (juego.Ganador != null)
                {
                    _ = Task.Run(async () => await _signalRService.NotifyGameEnded(codigoSala, juego.Ganador.Username));
                }

                _logger.LogInformation("Juego terminado en sala {CodigoSala}. Ganador: {Ganador}", 
                    codigoSala, juego.Ganador?.Username);
            }
            else
            {
                // Siguiente turno
                juego.TurnoActual = (juego.TurnoActual + 1) % juego.Usuarios.Count;
                
                // Saltar usuarios eliminados
                while (!juego.UsuarioTurnoActual.SigueEnJuego && !juego.JuegoTerminado)
                {
                    juego.TurnoActual = (juego.TurnoActual + 1) % juego.Usuarios.Count;
                }

                // ?? Notificar cambio de turno via SignalR
                if (!juego.JuegoTerminado)
                {
                    _ = Task.Run(async () => await _signalRService.NotifyTurnChange(codigoSala, turnoAnterior, juego.UsuarioTurnoActual.Username));
                }
            }

            // ?? Notificar actualización general del juego
            var gameUpdate = new
            {
                codigoSala = juego.CodigoSala,
                estado = juego.Estado.ToString(),
                juegoTerminado = juego.JuegoTerminado,
                ganador = juego.Ganador?.Username,
                turnoActual = juego.JuegoTerminado ? null : juego.UsuarioTurnoActual.Username,
                usuarios = juego.Usuarios.Select(u => new
                {
                    username = u.Username,
                    puntos = u.Puntos,
                    sigueEnJuego = u.SigueEnJuego,
                    casillasBarco = u.Barco.Count
                }),
                tablero = ObtenerEstadoTablero(codigoSala)
            };

            _ = Task.Run(async () => await _signalRService.NotifyGameUpdate(codigoSala, gameUpdate));

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar disparo en sala {CodigoSala}", codigoSala);
            return null;
        }
    }

    public string ObtenerEstadoTablero(string codigoSala)
    {
        var juego = ObtenerJuego(codigoSala);
        if (juego == null) return string.Empty;

        var tableroString = "   0 1 2 3 4 5 6 7 8 9\n";
        
        for (int i = 0; i < 10; i++)
        {
            tableroString += $"{i}  ";
            for (int j = 0; j < 10; j++)
            {
                tableroString += juego.Tablero[i, j] + " ";
            }
            tableroString += "\n";
        }

        return tableroString;
    }

    public List<string> ObtenerEstadisticas(string codigoSala)
    {
        var juego = ObtenerJuego(codigoSala);
        if (juego == null) return new List<string>();

        var stats = new List<string>
        {
            $"?? Sala: {juego.CodigoSala}",
            $"? Iniciado: {juego.IniciadoEn:HH:mm:ss}",
            $"?? Estado: {juego.Estado}",
            ""
        };

        if (juego.Estado == EstadoJuego.Finalizado && juego.Ganador != null)
        {
            stats.Add($"?? GANADOR: {juego.Ganador.Username}");
            stats.Add("");
        }
        else if (juego.Estado == EstadoJuego.EnCurso)
        {
            stats.Add($"?? Turno actual: {juego.UsuarioTurnoActual.Username}");
            stats.Add("");
        }

        stats.Add("?? Usuarios:");
        foreach (var usuario in juego.Usuarios.OrderByDescending(u => u.Puntos))
        {
            var estado = usuario.SigueEnJuego ? "??" : "??";
            stats.Add($"{estado} {usuario.Username}: {usuario.Puntos} puntos, {usuario.Barco.Count} casillas");
        }

        return stats;
    }
}