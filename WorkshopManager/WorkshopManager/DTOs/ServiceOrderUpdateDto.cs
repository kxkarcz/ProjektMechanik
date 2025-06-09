using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs
{
    public class ServiceOrderUpdateDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = null!;

        [Required]
        public int VehicleId { get; set; }

        public string? AssignedMechanicId { get; set; }

        public List<int> ServiceTaskIds { get; set; } = new List<int>();
    }
}
