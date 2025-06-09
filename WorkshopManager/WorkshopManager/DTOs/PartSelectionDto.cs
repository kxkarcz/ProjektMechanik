using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs
{
    public class PartSelectionDto
    {
        [Required]
        public int id { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Ilość musi być większa od 0")]
        public int quantity { get; set; }

        public string? name { get; set; }
        public decimal unitPrice { get; set; }
    }
}