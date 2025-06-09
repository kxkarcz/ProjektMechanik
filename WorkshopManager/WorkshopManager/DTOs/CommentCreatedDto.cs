using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.DTOs
{
    public class CommentCreatedDto
    {
        [Required(ErrorMessage = "Autor komentarza jest wymagany")]
        [StringLength(100, ErrorMessage = "Nazwa autora nie może być dłuższa niż 100 znaków")]
        [Display(Name = "Autor")]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Treść komentarza jest wymagana")]
        [StringLength(1000, MinimumLength = 5, ErrorMessage = "Komentarz musi mieć od 5 do 1000 znaków")]
        [Display(Name = "Treść komentarza")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "ID zlecenia jest wymagane")]
        public int ServiceOrderId { get; set; }
    }
}