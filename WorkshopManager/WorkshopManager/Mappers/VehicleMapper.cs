using WorkshopManager.DTOs;
using WorkshopManager.Models;
using Riok.Mapperly.Abstractions;

namespace WorkshopManager.Mappers
{
    [Mapper]
    public partial class VehicleMapper
    {
        public partial VehicleDto ToDto(Vehicle c);
        public partial Vehicle FromDto(VehicleCreateDto dto);
        public partial void UpdateEntity(VehicleUpdateDto dto, Vehicle vehicle);
    }
}