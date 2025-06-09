using WorkshopManager.Models;
using Xunit;
using Assert = Xunit.Assert;
public class ServiceTaskTests
{
    [Fact]
    public void CanSetAndGetProperties()
    {
        var task = new ServiceTask
        {
            Id = 1,
            Description = "Replace oil",
            LaborCost = 50.0m,
            ServiceOrderId = 2,
            ServiceOrder = new ServiceOrder()
        };

        Assert.Equal(1, task.Id);
        Assert.Equal("Replace oil", task.Description);
        Assert.Equal(50.0m, task.LaborCost);
        Assert.Equal(2, task.ServiceOrderId);
        Assert.NotNull(task.ServiceOrder);
    }

    [Fact]
    public void UsedPartsCollection_IsInitialized()
    {
        var task = new ServiceTask();
        Assert.NotNull(task.UsedParts);
        Assert.Empty(task.UsedParts);
    }
}
