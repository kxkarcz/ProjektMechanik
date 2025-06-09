using WorkshopManager.DTOs;

namespace WorkshopManager.Services
{
    public interface IPartService
    {
        Task<List<PartDto>> GetAllPartsAsync();
        Task<PartDto> GetPartByIdAsync(int id);
        Task<List<PartDto>> SearchPartsAsync(string searchTerm);
        Task<PartDto> CreatePartAsync(PartCreateDto partDto);
        Task<PartDto> UpdatePartAsync(int id, PartUpdateDto partDto);
        Task<bool> DeletePartAsync(int id);
    }
}
