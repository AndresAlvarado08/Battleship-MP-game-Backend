using Microsoft.AspNetCore.Http;

namespace Battlefield_Multiplayer_game_.NET.Services;

public interface ICookieService
{
    void SetSecureCookie(string name, string value, int expireMinutes);
    void SetSecureCookie(string name, string value, DateTime expires);
    string? GetCookie(string name);
    void DeleteCookie(string name);
    void SetAuthCookies(string accessToken, string refreshToken);
    void ClearAuthCookies();
}

public class CookieService : ICookieService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CookieService> _logger;
    private readonly bool _isProduction;

    public CookieService(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment, ILogger<CookieService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _isProduction = environment.IsProduction();
    }

    public void SetSecureCookie(string name, string value, int expireMinutes)
    {
        SetSecureCookie(name, value, DateTime.UtcNow.AddMinutes(expireMinutes));
    }

    public void SetSecureCookie(string name, string value, DateTime expires)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = _isProduction,
                SameSite = SameSiteMode.Strict,
                Expires = expires,
                Path = "/",
                Domain = null
            };

            context.Response.Cookies.Append(name, value, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error estableciendo cookie {CookieName}", name);
        }
    }

    public string? GetCookie(string name)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.Request.Cookies[name];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo cookie {CookieName}", name);
            return null;
        }
    }

    public void DeleteCookie(string name)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = _isProduction,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(-1),
                Path = "/",
                Domain = null
            };

            context.Response.Cookies.Append(name, "", options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando cookie {CookieName}", name);
        }
    }

    public void SetAuthCookies(string accessToken, string refreshToken)
    {
        SetSecureCookie("accessToken", accessToken, 60);

        SetSecureCookie("refreshToken", refreshToken, DateTime.UtcNow.AddDays(7));
    }

    public void ClearAuthCookies()
    {
        DeleteCookie("accessToken");
        DeleteCookie("refreshToken");
    }
}