using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs
{
    public class ServiceOrderDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = null!;

        [Required]
        public int VehicleId { get; set; }

        public string? AssignedMechanicId { get; set; }

        public List<int> ServiceTaskIds { get; set; } = new List<int>();
        public List<int> CommentIds { get; set; } = new List<int>();

        // Dodatkowe właściwości dla wyświetlania
        public DateTime CreatedAt { get; set; }
        public string VehicleBrand { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public string VehicleLicensePlate { get; set; } = string.Empty;
        public string AssignedMechanicName { get; set; } = string.Empty;

        // Właściwości dla szczegółów
        public string VehicleInfo => $"{VehicleBrand} {VehicleModel} ({VehicleLicensePlate})";
        public string StatusDisplayName => Status switch
        {
            "New" => "Nowe",
            "Open" => "Otwarte",
            "InProgress" => "W trakcie",
            "Completed" => "Zakończone",
            "Cancelled" => "Anulowane",
            _ => Status
        };
    }
}