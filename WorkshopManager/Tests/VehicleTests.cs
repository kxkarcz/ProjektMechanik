using WorkshopManager.Models;
using Xunit;
using Assert = Xunit.Assert;
public class VehicleTests
{
    [Fact]
    public void CanSetAndGetProperties()
    {
        var vehicle = new Vehicle
        {
            Id = 1,
            VIN = "1HGCM82633A004352",
            RegistrationNumber = "ABC123",
            ImageUrl = "http://example.com/car.jpg",
            CustomerId = 2,
            Customer = new Customer()
        };

        Assert.Equal(1, vehicle.Id);
        Assert.Equal("1HGCM82633A004352", vehicle.VIN);
        Assert.Equal("ABC123", vehicle.RegistrationNumber);
        Assert.Equal("http://example.com/car.jpg", vehicle.ImageUrl);
        Assert.Equal(2, vehicle.CustomerId);
        Assert.NotNull(vehicle.Customer);
    }

    [Fact]
    public void ServiceOrdersCollection_IsInitialized()
    {
        var vehicle = new Vehicle();
        Assert.NotNull(vehicle.ServiceOrders);
        Assert.Empty(vehicle.ServiceOrders);
    }
}
