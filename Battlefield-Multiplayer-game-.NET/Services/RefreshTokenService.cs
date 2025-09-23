using System.Collections.Concurrent;
using Battlefield_Multiplayer_game_.NET.Models;

namespace Battlefield_Multiplayer_game_.NET.Services;

public interface IRefreshTokenService
{
    Task<RefreshToken> CreateRefreshTokenAsync(string username, string? deviceInfo = null, string? ipAddress = null);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<bool> ValidateRefreshTokenAsync(string token, string username);
    Task RevokeRefreshTokenAsync(string token);
    Task RevokeAllUserRefreshTokensAsync(string username);
    Task CleanupExpiredTokensAsync();
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ConcurrentDictionary<string, RefreshToken> _refreshTokens = new();
    private readonly IConfiguration _configuration;
    private readonly Timer _cleanupTimer;

    public RefreshTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        _cleanupTimer = new Timer(async _ => await CleanupExpiredTokensAsync(), 
            null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
    }

    public Task<RefreshToken> CreateRefreshTokenAsync(string username, string? deviceInfo = null, string? ipAddress = null)
    {
        var expireDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpireDays", 7);
        
        var refreshToken = new RefreshToken
        {
            Token = GenerateSecureToken(),
            Username = username,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(expireDays),
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            IsRevoked = false
        };

        _refreshTokens[refreshToken.Token] = refreshToken;
        return Task.FromResult(refreshToken);
    }

    public Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        _refreshTokens.TryGetValue(token, out var refreshToken);
        return Task.FromResult(refreshToken);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string token, string username)
    {
        var refreshToken = await GetRefreshTokenAsync(token);
        return refreshToken != null && 
               refreshToken.Username == username && 
               refreshToken.IsActive;
    }

    public Task RevokeRefreshTokenAsync(string token)
    {
        if (_refreshTokens.TryGetValue(token, out var refreshToken))
        {
            refreshToken.IsRevoked = true;
        }
        return Task.CompletedTask;
    }

    public Task RevokeAllUserRefreshTokensAsync(string username)
    {
        var userTokens = _refreshTokens.Values.Where(rt => rt.Username == username);
        foreach (var token in userTokens)
        {
            token.IsRevoked = true;
        }
        return Task.CompletedTask;
    }

    public Task CleanupExpiredTokensAsync()
    {
        var expiredTokens = _refreshTokens
            .Where(kvp => kvp.Value.IsExpired || kvp.Value.IsRevoked)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var tokenKey in expiredTokens)
        {
            _refreshTokens.TryRemove(tokenKey, out _);
        }

        return Task.CompletedTask;
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}