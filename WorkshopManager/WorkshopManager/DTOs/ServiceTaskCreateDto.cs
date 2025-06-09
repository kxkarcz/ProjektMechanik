namespace WorkshopManager.DTOs
{
    public class ServiceTaskCreateDto
    {
        public string Description { get; set; } = null!;
        public decimal LaborCost { get; set; }
        public int ServiceOrderId { get; set; }
    }
}
