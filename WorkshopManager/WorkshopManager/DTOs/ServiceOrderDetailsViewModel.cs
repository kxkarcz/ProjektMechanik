using WorkshopManager.DTOs;
using WorkshopManager.Models;


namespace WorkshopManager.ViewModels
{
    public class ServiceOrderDetailsViewModel
    {
        public ServiceOrderDto Order { get; set; } = null!;
        public List<CommentDto> Comments { get; set; } = new();
        public CommentCreatedDto NewComment { get; set; } = new();
        public List<UsedPart> UsedParts { get; set; } = new List<UsedPart>();
    }
}