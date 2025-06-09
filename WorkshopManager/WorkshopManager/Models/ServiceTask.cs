using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models
{
    public class ServiceTask
    {
        public int Id { get; set; }

        [Required, StringLength(500)]
        public string Description { get; set; } = null!;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Koszt robocizny musi być większy od 0")]
        public decimal LaborCost { get; set; }
        public int? ServiceOrderId { get; set; }
        public ServiceOrder? ServiceOrder { get; set; }
        public List<UsedPart> UsedParts { get; set; } = new List<UsedPart>();
    }
}