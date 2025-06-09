using WorkshopManager.DTOs;


namespace WorkshopManager.Services
{
    public interface IUsedPartService
    {
        Task<UsedPartDto> AddPartToTaskAsync(UsedPartCreateDto usedPartDto);
        Task<bool> RemovePartFromTaskAsync(int usedPartId);
        Task<bool> UpdatePartQuantityAsync(int usedPartId, int newQuantity);
        Task<UsedPartDto> GetUsedPartByIdAsync(int usedPartId);
    }
}
