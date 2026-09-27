namespace MicrogridApi.Dtos;

public class CreateSlotRequest
{
    public string StationId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int Capacity { get; set; }
}
