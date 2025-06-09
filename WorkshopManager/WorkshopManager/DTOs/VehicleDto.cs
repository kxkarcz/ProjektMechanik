using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs
{
    public class VehicleDto
    {
        public int Id { get; set; }

        public string VIN { get; set; } = null!;

        public string RegistrationNumber { get; set; } = null!;

        public string ImageUrl { get; set; } = null!;

        public int CustomerId { get; set; }
    }
}
