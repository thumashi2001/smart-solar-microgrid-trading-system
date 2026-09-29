// =============================================================================
// File: LoginRequest.cs
// Description: Request body for POST api/auth/login. Identifier accepts an
//              email (Backoffice/GridOperator) or a NIC (Prosumer).
// Author: Thumashi (Component 1)
// =============================================================================

namespace MicrogridApi.Dtos;

public class LoginRequest
{
    public string Identifier { get; set; } = string.Empty; // email or NIC
    public string Password { get; set; } = string.Empty;
}