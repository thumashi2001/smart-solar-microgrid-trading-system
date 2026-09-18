namespace MicrogridApi.Dtos;

public class LoginRequest
{
    public string Identifier { get; set; } = string.Empty; // email or NIC
    public string Password { get; set; } = string.Empty;
}