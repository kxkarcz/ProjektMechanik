using WorkshopManager.DTOs;
using WorkshopManager.Models;
using Riok.Mapperly.Abstractions;

namespace WorkshopManager.Mappers
{
    [Mapper]
    public partial class ServiceTaskMapper
    {
        public partial ServiceTaskDto ToDto(ServiceTask entity);
        public partial ServiceTask FromDto(ServiceTaskCreateDto dto);
        public partial void UpdateEntity(ServiceTaskUpdateDto dto, ServiceTask entity);
    }

}
