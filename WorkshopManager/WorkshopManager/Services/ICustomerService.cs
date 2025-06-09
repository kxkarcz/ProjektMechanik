using WorkshopManager.DTOs;

namespace WorkshopManager.Services
{
    public interface ICustomerService
    {
        Task<List<CustomerDto>> GetAllAsync(string? filter);
        Task<CustomerDto?> GetByIdAsync(int id);
        Task CreateAsync(CustomerDto dto);
        Task UpdateAsync(CustomerDto dto);
        Task DeleteAsync(int id);
    }
}
