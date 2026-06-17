namespace Gatx.Application.Common.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "gatx";
    public string Audience { get; set; } = "gatx";
    public int ExpiresMinutes { get; set; } = 480;
}
