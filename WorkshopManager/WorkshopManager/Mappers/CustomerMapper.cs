using Riok.Mapperly.Abstractions;
using WorkshopManager.Models;
using WorkshopManager.DTOs;

namespace WorkshopManager.Mappers
{
    [Mapper]
    public partial class CustomerMapper
    {
        public partial CustomerDto ToDto(Customer c);
        public partial Customer FromDto(CustomerDto dto);
    }
}
