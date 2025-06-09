using System.ComponentModel.DataAnnotations;

namespace WorkshopManager.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        [Required, StringLength(17)]
        public string VIN { get; set; } = null!;
        [Required, StringLength(20)]
        public string RegistrationNumber { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        [Required]
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        public List<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();
    }
}
