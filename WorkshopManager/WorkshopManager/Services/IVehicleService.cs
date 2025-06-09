using WorkshopManager.DTOs;

namespace WorkshopManager.Services
{
    public interface IVehicleService
    {
        Task<List<VehicleDto>> GetAllVehiclesAsync();
        Task<VehicleDto> GetVehicleByIdAsync(int id);
        Task<List<VehicleDto>> GetVehiclesByCustomerIdAsync(int customerId);
        Task<VehicleDto> CreateVehicleAsync(VehicleCreateDto vehicleDto);
        Task<VehicleDto> UpdateVehicleAsync(int id, VehicleUpdateDto vehicleDto);
        Task<bool> DeleteVehicleAsync(int id);
        Task<string> UploadVehicleImageAsync(int vehicleId, IFormFile image);
    }
}
