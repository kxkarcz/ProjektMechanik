namespace WorkshopManager.DTOs
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string Author { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public int ServiceOrderId { get; set; }
    }
}
