using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models
{
    public class Comment
    {
        public int Id { get; set; }
        [Required]
        public string Author { get; set; } = null!;
        [Required]
        public string Content { get; set; } = null!;
        [Required]
        public DateTime Timestamp { get; set; }
        public int ServiceOrderId { get; set; }
        public ServiceOrder ServiceOrder { get; set; } = null!;
    }
}
