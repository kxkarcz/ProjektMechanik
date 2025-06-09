using WorkshopManager.Models;
using Xunit;
using Assert = Xunit.Assert;
public class UsedPartTests
{
    [Fact]
    public void CanSetAndGetProperties()
    {
        var usedPart = new UsedPart
        {
            Id = 1,
            Quantity = 3,
            PartId = 2,
            Part = new Part()
        };

        Assert.Equal(1, usedPart.Id);
        Assert.Equal(3, usedPart.Quantity);
        Assert.Equal(2, usedPart.PartId);
        Assert.NotNull(usedPart.Part);
    }
}
