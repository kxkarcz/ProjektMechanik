using Riok.Mapperly.Abstractions;
using WorkshopManager.DTOs;
using WorkshopManager.Models;

[Mapper]
public partial class ServiceOrderMapper
{
    public partial ServiceOrderDto ToDto(ServiceOrder entity);

    [MapProperty(nameof(ServiceOrder.ServiceTasks), nameof(ServiceOrderDto.ServiceTaskIds))]
    public ServiceOrderDto ToDtoWithTasks(ServiceOrder entity)
    {
        var dto = ToDto(entity);
        dto.ServiceTaskIds = entity.ServiceTasks?.Select(t => t.Id).ToList() ?? new List<int>();
        dto.CommentIds = entity.Comments?.Select(c => c.Id).ToList() ?? new List<int>();
        return dto;
    }

    public ServiceOrder ToEntity(ServiceOrderCreateDto dto)
    {
        return new ServiceOrder
        {
            VehicleId = dto.VehicleId,
            AssignedMechanicId = dto.AssignedMechanicId,
            Status = dto.Status,
            ServiceTasks = new List<ServiceTask>(),
            Comments = new List<Comment>()
        };
    }

    public void UpdateEntity(ServiceOrderUpdateDto dto, ServiceOrder entity)
    {
        entity.Status = dto.Status;
        entity.VehicleId = dto.VehicleId;
        entity.AssignedMechanicId = dto.AssignedMechanicId;
    }
}