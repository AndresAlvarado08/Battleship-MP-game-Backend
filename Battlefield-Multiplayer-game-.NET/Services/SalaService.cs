namespace Battlefield_Multiplayer_game_.NET.Services;

using Models;
using System.Collections.Concurrent;


public class SalaService : ISalaService
{
    private readonly ConcurrentDictionary<string, Sala> _salas = new();
    private readonly Random _random = new();


    public Sala CreateSala(string creatorUsername)
    {
        string codigo = GenerateCode();

        var sala = new Sala
        {
            Codigo = codigo,
            Jugadores = new List<string> { creatorUsername },
            Estado = "PrePartida"
        };

        _salas[codigo] = sala;
        return sala;
    }

    public Sala? JoinSala(string codigo, string username)
    {
        if (_salas.TryGetValue(codigo, out var sala))
        {
            // Si la sala ya tiene 2 jugadores, no se puede unir nadie más
            if (sala.Jugadores.Count >= 2)
            {
                return null;
            }
            if (!sala.Jugadores.Contains(username))
            {
                sala.Jugadores.Add(username);
                // Si ahora hay 2 jugadores, cambia el estado a "En curso"
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
        _salas.TryRemove(codigo, out _);
    }

    private string GenerateCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}


