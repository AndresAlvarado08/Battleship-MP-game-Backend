namespace Battlefield_Multiplayer_game_.NET.Models;

public class Usuario
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
    
    // Propiedades del juego Warship
    public List<(int fila, int columna)> Barco { get; set; } = new();

    public int Puntos { get; set; } = 0;
    
    // Propiedad calculada para saber si sigue en juego
    public bool SigueEnJuego => Barco.Count > 0;
}
