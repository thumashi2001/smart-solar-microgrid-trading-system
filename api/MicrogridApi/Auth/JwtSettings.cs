// =============================================================================
// File: JwtSettings.cs
// Description: JWT configuration bound from local appsettings / environment.
// Author: Suwani (Component 4) — shared auth change for Thumashi review
// =============================================================================

namespace MicrogridApi.Auth;

public class JwtSettings
{
    public string Issuer { get; set; } = "MicrogridApi";
    public string Audience { get; set; } = "MicrogridClients";
    public string SecretKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 480;
}
