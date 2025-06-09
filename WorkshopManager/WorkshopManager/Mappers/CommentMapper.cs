using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers
{
    [Mapper]
    public partial class CommentMapper
    {
        public partial CommentDto ToDto(Comment comment);
        public partial Comment FromDto(CommentCreatedDto dto);  
        }
}