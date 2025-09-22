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
        var u = new Usuario
        {
            Username = username,
            Password = hash
        };

        user[username] = u;
        return u;
    }

    public Usuario? Login(string username, string password)
    {
        if (!user.TryGetValue(username, out var u)) return null;
        if (!VerifyPassword(password, u.Password)) return null;
        return u;
    }

    public Usuario? GetbyUser(string username)
    {
        user.TryGetValue(username, out var u);
        return u;
    }

    // Nuevo: retornar todos los usuarios
    public IEnumerable<Usuario> GetAll()
    {
        // Nota: devolvemos la colección interna (solo lectura en IEnumerable)
        // La sanitización de salida (ocultar password) se hace en el controller.
        return user.Values;
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
