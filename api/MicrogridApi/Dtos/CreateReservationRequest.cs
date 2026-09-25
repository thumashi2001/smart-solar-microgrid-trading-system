using System.ComponentModel.DataAnnotations;

namespace MicrogridApi.Dtos;

public class CreateReservationRequest
{
    [Required]
    [RegularExpression(@"^([0-9]{9}[vVxX]|[0-9]{12})$", ErrorMessage = "Invalid NIC format.")]
    public string ProsumerNic { get; set; } = string.Empty;
    public string StationId { get; set; } = string.Empty;
    public string SlotId { get; set; } = string.Empty;
}
