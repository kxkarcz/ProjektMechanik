using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WorkshopManager.Models
{
    public class Part
    {
        public int Id { get; set; }
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;
        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cena musi być większa od 0")]
        public decimal UnitPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stan magazynowy nie może być ujemny")]
        public int StockQuantity { get; set; } = 0;

        [StringLength(500)]
        public string? Description { get; set; }

        public List<UsedPart> UsedParts { get; set; } = new List<UsedPart>();
    }
}