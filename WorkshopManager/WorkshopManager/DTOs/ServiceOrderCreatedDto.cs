using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs
{
    public class ServiceOrderCreateDto
    {
        [Required(ErrorMessage = "Pole VehicleId jest wymagane")]
        [Range(1, int.MaxValue, ErrorMessage = "VehicleId musi być większe od 0")]
        public int VehicleId { get; set; }

        public List<int> ServiceTaskIds { get; set; } = new();

        [StringLength(36, ErrorMessage = "AssignedMechanicId nie może być dłuższe niż 36 znaków")]
        public string AssignedMechanicId { get; set; } = "8a9c2f33-1d24-4c83-9d92-5ebf9f8327b2";

        [StringLength(50, ErrorMessage = "Status nie może być dłuższy niż 50 znaków")]
        public string Status { get; set; } = "Open";
    }
}