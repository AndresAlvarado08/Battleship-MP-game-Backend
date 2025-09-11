namespace Battlefield_Multiplayer_game_.NET.Models;

public class Sala
{
    public string Codigo { get; set; }

    public List<string> Jugadores { get; set; } = new();

    public string Estado { get; set; } = "PrePartida"; // En curso y finalizada

}
