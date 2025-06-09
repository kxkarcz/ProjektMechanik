using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs
{
    public class NewTaskDto
    {
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = null!;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Koszt robocizny musi być większy od 0")]
        public decimal LaborCost { get; set; }

        public int ServiceOrderId { get; set; }
    }
}