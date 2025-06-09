using WorkshopManager.DTOs;

namespace WorkshopManager.Services
{
    public interface IServiceOrderService
    {
        Task<List<ServiceOrderDto>> GetAllOrdersAsync();
        Task<ServiceOrderDto> GetOrderByIdAsync(int id);
        Task<List<ServiceOrderDto>> GetOrdersByStatusAsync(string status);
        Task<List<ServiceOrderDto>> GetOrdersByMechanicIdAsync(string mechanicId);
        Task<List<ServiceOrderDto>> GetOrdersByVehicleIdAsync(int vehicleId);
        Task<List<ServiceOrderDto>> GetOrdersByCustomerIdAsync(int customerId);
        Task<ServiceOrderDto> CreateOrderAsync(ServiceOrderCreateDto orderDto);
        Task<ServiceOrderDto> UpdateOrderAsync(int id, ServiceOrderUpdateDto orderDto);
        Task<ServiceOrderDto> UpdateOrderStatusAsync(int id, string newStatus);
        Task<bool> DeleteOrderAsync(int id);
        Task<ServiceOrderDto> AssignMechanicAsync(int orderId, string mechanicId);
        Task<byte[]> GenerateOrderPdfAsync(int orderId);
    }

}
