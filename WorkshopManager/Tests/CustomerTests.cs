using System.Collections.Generic;
using WorkshopManager.Models;
using Xunit;
using Assert = Xunit.Assert;
public class CustomerTests
{
    [Fact]
    public void CanSetAndGetProperties()
    {
        var customer = new Customer
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            PhoneNumber = "123456789"
        };

        Assert.Equal(1, customer.Id);
        Assert.Equal("Alice", customer.FirstName);
        Assert.Equal("Smith", customer.LastName);
        Assert.Equal("alice@example.com", customer.Email);
        Assert.Equal("123456789", customer.PhoneNumber);
    }

    [Fact]
    public void VehiclesCollection_IsInitialized()
    {
        var customer = new Customer();
        Assert.NotNull(customer.Vehicles);
        Assert.Empty(customer.Vehicles);
    }
}
