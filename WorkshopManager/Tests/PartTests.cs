using WorkshopManager.Models;
using Xunit;
using Assert = Xunit.Assert;
public class PartTests
{
    [Fact]
    public void CanSetAndGetProperties()
    {
        var part = new Part
        {
            Id = 1,
            Name = "Brake Pad",
            UnitPrice = 99.99m
        };

        Assert.Equal(1, part.Id);
        Assert.Equal("Brake Pad", part.Name);
        Assert.Equal(99.99m, part.UnitPrice);
    }

    [Fact]
    public void UsedPartsCollection_IsInitialized()
    {
        var part = new Part();
        Assert.NotNull(part.UsedParts);
        Assert.Empty(part.UsedParts);
    }
}
