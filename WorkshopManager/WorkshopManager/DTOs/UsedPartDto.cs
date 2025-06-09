using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs
{
    public class UsedPartDto
    {
        public int Id { get; set; }

        [Required]
        public int PartId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Ilość musi być większa od 0")]
        public int Quantity { get; set; }

        [Required]
        public int ServiceOrderId { get; set; }

        // Dodatowe właściwości dla wyświetlania
        public string PartName { get; set; } = string.Empty;
        public decimal PartPrice { get; set; }
        public int PartStockQuantity { get; set; }
    }
}