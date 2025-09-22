namespace Battlefield_Multiplayer_game_.NET.Services;

using Models;
public interface IUserService
{
    Usuario? Register(string username , string pasword);
    Usuario? Login(string username, string password);
    Usuario? GetbyUser(string username);
}
