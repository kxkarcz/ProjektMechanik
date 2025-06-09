using System.ComponentModel.DataAnnotations;
namespace WorkshopManager.Models
{
    public class UsedPart
    {
        public int Id { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Ilość musi być większa od 0")]
        public int Quantity { get; set; }

        [Required]
        public int PartId { get; set; }
        public Part Part { get; set; } = null!;
        public int? ServiceOrderId { get; set; }
        public ServiceOrder? ServiceOrder { get; set; }
    }
}