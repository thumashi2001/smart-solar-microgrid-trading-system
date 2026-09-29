// =============================================================================
// File: LoginResponse.cs
// Description: Response body returned by POST api/auth/login, containing the
//              JWT token, the user's role, display name and NIC (Prosumers).
// Author: Thumashi (Component 1)
// =============================================================================

namespace MicrogridApi.Dtos;

public class LoginResponse
{
    public string Token { get; set; } = "";
    public string Role { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Nic { get; set; } = "";
}