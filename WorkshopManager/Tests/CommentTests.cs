using WorkshopManager.Models;
using Xunit;
using Assert = Xunit.Assert;
public class CommentTests
{
    [Fact]
    public void CanSetAndGetProperties()
    {
        var comment = new Comment
        {
            Id = 1,
            Author = "John",
            Content = "Test comment",
            Timestamp = new DateTime(2024, 1, 1, 12, 0, 0),
            ServiceOrderId = 42,
            ServiceOrder = new ServiceOrder()
        };

        Assert.Equal(1, comment.Id);
        Assert.Equal("John", comment.Author);
        Assert.Equal("Test comment", comment.Content);
        Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0), comment.Timestamp);
        Assert.Equal(42, comment.ServiceOrderId);
        Assert.NotNull(comment.ServiceOrder);
    }
}
