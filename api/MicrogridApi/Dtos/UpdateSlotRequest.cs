namespace MicrogridApi.Dtos;

public class UpdateSlotRequest
{
    public string Status { get; set; } = string.Empty; // e.g. "Available", "Unavailable"
}
