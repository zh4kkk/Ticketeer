namespace Ticketeer.Services;

public sealed class JwtOptions
{
    public required string Key { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public int ExpiresMinutes { get; init; }
}
