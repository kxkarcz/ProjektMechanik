using WorkshopManager.Models;
using Xunit;
using Assert = Xunit.Assert;
public class ServiceOrderTests
{
    [Fact]
    public void CanSetAndGetProperties()
    {
        var order = new ServiceOrder
        {
            Id = 1,
            Status = "Open",
            AssignedMechanicId = "mech1",
            VehicleId = 2,
            Vehicle = new Vehicle()
        };

        Assert.Equal(1, order.Id);
        Assert.Equal("Open", order.Status);
        Assert.Equal("mech1", order.AssignedMechanicId);
        Assert.Equal(2, order.VehicleId);
        Assert.NotNull(order.Vehicle);
    }

    [Fact]
    public void ServiceTasksCollection_IsInitialized()
    {
        var order = new ServiceOrder();
        Assert.NotNull(order.ServiceTasks);
        Assert.Empty(order.ServiceTasks);
    }

    [Fact]
    public void CommentsCollection_IsInitialized()
    {
        var order = new ServiceOrder();
        Assert.NotNull(order.Comments);
        Assert.Empty(order.Comments);
    }
}
