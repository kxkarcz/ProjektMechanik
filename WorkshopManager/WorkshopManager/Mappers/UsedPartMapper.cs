using Riok.Mapperly.Abstractions;
using WorkshopManager.DTOs;
using WorkshopManager.DTOs;
using WorkshopManager.Models;

namespace WorkshopManager.Mappers
{
    [Mapper]
    public partial class UsedPartMapper
    {
        [MapProperty(nameof(UsedPart.Part.Name), nameof(UsedPartDto.PartName))]
        [MapProperty(nameof(UsedPart.Part.UnitPrice), nameof(UsedPartDto.PartPrice))]
        public partial UsedPartDto ToDto(UsedPart usedPart);

        public partial UsedPart FromCreateDto(UsedPartCreateDto dto);
    }
}
