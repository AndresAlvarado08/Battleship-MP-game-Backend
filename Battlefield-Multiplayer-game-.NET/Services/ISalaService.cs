namespace Battlefield_Multiplayer_game_.NET.Services;
using Models;

public interface ISalaService
{
    Sala CreateSala(string creatorUsername);
    Sala? JoinSala(string codigo, string username);
    Sala? GetSala(string codigo);
    List<Sala> GelALLSalas();
    void EliminarSala(string codigo);
}
