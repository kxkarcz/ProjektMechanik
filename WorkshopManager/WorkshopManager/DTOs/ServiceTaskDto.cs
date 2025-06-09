using System;

namespace WorkshopManager.DTOs
{
    public class ServiceTaskDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = null!;
        public decimal LaborCost { get; set; }
        public int ServiceOrderId { get; set; }
        public string ServiceOrderStatus { get; set; } = null!;
    }
}
