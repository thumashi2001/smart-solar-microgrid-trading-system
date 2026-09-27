namespace MicrogridApi.Dtos;

public class UpdateSlotRequest
{
    public DateTime? Date { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string Status { get; set; } = string.Empty; // e.g. "Available", "Unavailable"
}
