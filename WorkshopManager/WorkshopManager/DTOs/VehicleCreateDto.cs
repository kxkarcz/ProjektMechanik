using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs
{
    public class VehicleCreateDto
    {
        public string VIN { get; set; } = null!;
        public string RegistrationNumber { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public int CustomerId { get; set; }
    }

}
