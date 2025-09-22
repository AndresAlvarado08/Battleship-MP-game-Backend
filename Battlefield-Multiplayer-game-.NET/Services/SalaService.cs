namespace Battlefield_Multiplayer_game_.NET.Services;
using Models;
using System.Collections.Concurrent;

public class SalaService : ISalaService
{
    private readonly ConcurrentDictionary<string, Sala> _salas = new();
    private readonly ConcurrentDictionary<string, string> _usuarioSala = new();
    private readonly Random _random = new();

    public Sala CreateSala(string creatorUsername)
    {

        if (_usuarioSala.ContainsKey(creatorUsername))
          throw new InvalidOperationException("El usuario ya esta en una sala");

        string codigo = GenerateCode();

        var sala = new Sala
        {
            Codigo = codigo,
            Jugadores = new List<string> { creatorUsername },
            Estado = "PrePartida",
            Creador = creatorUsername 
        };

        _salas[codigo] = sala;
        _usuarioSala[creatorUsername] = codigo;
        return sala;
    }

    public Sala? JoinSala(string codigo, string username)
    {
        if (_usuarioSala.ContainsKey(username))
            throw new InvalidOperationException("El usuario ya esta en una sala");


        if (_salas.TryGetValue(codigo, out var sala))
        {
            // Si la sala ya tiene 2 jugadores no se puede unir nadie mas, solo para desarrollo
            if (sala.Jugadores.Count >= 2)
            {
                return null;
            }
            if (!sala.Jugadores.Contains(username))
            {
                sala.Jugadores.Add(username);
                // Si ahora hay 2 jugadores cambia el estado a "En curso"
                if (sala.Jugadores.Count == 2)
                {
                    sala.Estado = "En curso";
                }
            }
            return sala;
        }
        return null;
    }

    public Sala? GetSala(string codigo)
    {
        _salas.TryGetValue(codigo, out var sala);
        return sala;
    }

    public List<Sala> GelALLSalas()
    {
        return _salas.Values.ToList();
    }

    public void EliminarSala(string codigo)
    {
        if (_salas.TryRemove(codigo, out var sala))
        {
            // Elimina la relacion de los usuarios con la sala eliminada
            foreach (var jugador in sala.Jugadores)
            {
                _usuarioSala.TryRemove(jugador, out _);
            }
        }
    }

    public bool EliminarSala(string codigo, string username)
    {
        if (_salas.TryGetValue(codigo, out var sala))
        {
            if (sala.Creador != username)
                return false; // Solo el creador puede eliminar la sala

            if (_salas.TryRemove(codigo, out var salaEliminada))
            {
                foreach (var jugador in salaEliminada.Jugadores)
                {
                    _usuarioSala.TryRemove(jugador, out _);
                }
                return true;
            }
        }
        return false;
    }

    public void SalirDeSala(string codigo, string username)
    {
        if (_salas.TryGetValue(codigo, out var sala))
        {
            if (sala.Jugadores.Contains(username))
            {
                sala.Jugadores.Remove(username);
                _usuarioSala.TryRemove(username, out _);

                // Si la sala queda vacia se elimina
                if (sala.Jugadores.Count == 0)
                {
                    _salas.TryRemove(codigo, out _);
                }
                // Si la sala estaba en curso y ahora tiene menos de 2 jugadores vuelve a PrePartida
                else if (sala.Estado == "En curso" && sala.Jugadores.Count < 2)
                {
                    sala.Estado = "PrePartida";
                }
            }
        }
    }

    private string GenerateCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}