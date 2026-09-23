namespace MicrogridApi.Dtos;

public class CreateReservationRequest
{
    public string ProsumerNic { get; set; } = string.Empty;
    public string StationId { get; set; } = string.Empty;
    public string SlotId { get; set; } = string.Empty;
}
