using ImageMCP.Models;
using Xunit;

namespace ImageMCP.Tests.Models;

public class McpMessageTests
{
    [Fact]
    public void McpMessage_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var message = new McpMessage();

        // Assert
        Assert.Equal("2.0", message.JsonRpc);
        Assert.Null(message.Id);
        Assert.Null(message.Method);
        Assert.Null(message.Params);
        Assert.Null(message.Result);
        Assert.Null(message.Error);
    }

    [Fact]
    public void McpMessage_ShouldAllowSettingProperties()
    {
        // Arrange & Act
        var message = new McpMessage
        {
            Id = 123,
            Method = "test_method",
            Params = new { key = "value" }
        };

        // Assert
        Assert.Equal(123, message.Id);
        Assert.Equal("test_method", message.Method);
        Assert.NotNull(message.Params);
    }

    [Fact]
    public void McpError_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var error = new McpError
        {
            Code = -32600,
            Message = "Invalid Request",
            Data = new { detail = "Missing parameter" }
        };

        // Assert
        Assert.Equal(-32600, error.Code);
        Assert.Equal("Invalid Request", error.Message);
        Assert.NotNull(error.Data);
    }
}
