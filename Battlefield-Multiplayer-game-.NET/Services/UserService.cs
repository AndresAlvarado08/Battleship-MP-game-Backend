namespace Battlefield_Multiplayer_game_.NET.Services;

using Models;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

public class UserService : IUserService
{
    private readonly ConcurrentDictionary<string, Usuario> user = new();

    public Usuario? Register(string username, string password)
    {
        if (user.ContainsKey(username)) return null;

        var hash = HashPassword(password);
        var _user = new Usuario { Username = username, Password = hash };
        user[username] = _user;

        return _user;
    }

    public Usuario? Login(string username, string password)
    {
        if (user.TryGetValue(username, out var _user))
        {
            if (VerifyPassword(password, _user.Password))

                return _user;
        }
        return null;
    }

    private string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

}
