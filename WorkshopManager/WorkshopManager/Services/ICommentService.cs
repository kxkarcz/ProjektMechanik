using WorkshopManager.DTOs;

namespace WorkshopManager.Services
{
    public interface ICommentService
    {
        Task<List<CommentDto>> GetAllAsync();
        Task<List<CommentDto>> GetCommentsByOrderIdAsync(int orderId);
        Task<CommentDto> CreateCommentAsync(CommentCreatedDto commentDto);
        Task<CommentDto?> UpdateCommentAsync(int id, CommentCreatedDto updateDto);
        Task<bool> DeleteCommentAsync(int id);
    }
}
