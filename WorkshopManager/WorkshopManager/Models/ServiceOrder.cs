using System.ComponentModel.DataAnnotations;
namespace WorkshopManager.Models
{
    public class ServiceOrder
    {
        public int Id { get; set; }
        [Required, StringLength(50)]
        public string Status { get; set; } = null!;
        public string? AssignedMechanicId { get; set; }
        [Required]
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;
        public List<ServiceTask> ServiceTasks { get; set; } = new List<ServiceTask>();
        public List<Comment> Comments { get; set; } = new List<Comment>();

        public List<UsedPart> UsedParts { get; set; } = new List<UsedPart>();

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}