using WorkshopManager.DTOs;

namespace WorkshopManager.Services
{
    public interface IServiceTaskService
    {
        Task<List<ServiceTaskDto>> GetTasksByOrderIdAsync(int orderId);
        Task<ServiceTaskDto> GetTaskByIdAsync(int id);
        Task<ServiceTaskDto> CreateTaskAsync(ServiceTaskCreateDto taskDto);
        Task<ServiceTaskDto> UpdateTaskAsync(int id, ServiceTaskUpdateDto taskDto);
        Task<bool> DeleteTaskAsync(int id);
        Task<ServiceTaskDto> MarkTaskAsCompletedAsync(int id);
    }
}
