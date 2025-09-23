namespace Battlefield_Multiplayer_game_.NET.Models;

public class JuegoWarship
{
    public string CodigoSala { get; set; } = string.Empty;
    public List<Usuario> Usuarios { get; set; } = new();
    public char[,] Tablero { get; set; } = new char[10, 10];
    public int TurnoActual { get; set; } = 0;
    public EstadoJuego Estado { get; set; } = EstadoJuego.Preparando;
    public Usuario? Ganador { get; set; }
    public DateTime IniciadoEn { get; set; }
    public List<string> HistorialTurnos { get; set; } = new();
    
    public Usuario UsuarioTurnoActual => Usuarios[TurnoActual % Usuarios.Count];
    public List<Usuario> UsuariosActivos => Usuarios.Where(u => u.SigueEnJuego).ToList();
    public bool JuegoTerminado => UsuariosActivos.Count <= 1;
}

public enum EstadoJuego
{
    Preparando,
    EnCurso,
    Finalizado
}

public class DisparoResultado
{
    public bool Acierto { get; set; }
    public string UsuarioImpactado { get; set; } = string.Empty;
    public bool UsuarioEliminado { get; set; }
    public int Fila { get; set; }
    public int Columna { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}