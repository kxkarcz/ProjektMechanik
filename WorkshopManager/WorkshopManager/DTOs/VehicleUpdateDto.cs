using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs
{
    public class VehicleUpdateDto
    {
        public string VIN { get; set; } = null!;
        public string RegistrationNumber { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public int CustomerId { get; set; }
    }

}
