namespace Battlefield_Multiplayer_game_.NET.Configuration;

public class JwtSettings
{
    public const string SectionName = "Jwt";
    
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpireMinutes { get; set; } = 15;
    public int RefreshTokenExpireDays { get; set; } = 7;
    public int ClockSkewMinutes { get; set; } = 5;
}