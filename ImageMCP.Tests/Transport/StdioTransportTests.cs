using ImageMCP.Transport;
using ImageMCP.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ImageMCP.Tests.Transport;

public class StdioTransportTests
{
    private readonly Mock<ILogger<StdioTransport>> _mockLogger;

    public StdioTransportTests()
    {
        _mockLogger = new Mock<ILogger<StdioTransport>>();
    }

    [Fact]
    public async Task ReadMessageAsync_ShouldDeserializeValidJson()
    {
        // Arrange
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "test_method",
            Id = 1
        };
        var json = JsonSerializer.Serialize(message) + "\n";
        var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var outputStream = new MemoryStream();
        
        using var transport = new StdioTransport(_mockLogger.Object, inputStream, outputStream);

        // Act
        var result = await transport.ReadMessageAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2.0", result.JsonRpc);
        Assert.Equal("test_method", result.Method);
        Assert.Equal(1, ((JsonElement)result.Id!).GetInt32());
    }

    [Fact]
    public async Task ReadMessageAsync_ShouldReturnNullForEmptyLine()
    {
        // Arrange
        var inputStream = new MemoryStream(Encoding.UTF8.GetBytes("\n"));
        var outputStream = new MemoryStream();
        
        using var transport = new StdioTransport(_mockLogger.Object, inputStream, outputStream);

        // Act
        var result = await transport.ReadMessageAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task WriteMessageAsync_ShouldSerializeMessage()
    {
        // Arrange
        var message = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "test_method",
            Id = 1
        };
        var inputStream = new MemoryStream();
        var outputStream = new MemoryStream();
        
        using var transport = new StdioTransport(_mockLogger.Object, inputStream, outputStream);

        // Act
        await transport.WriteMessageAsync(message);

        // Assert
        outputStream.Position = 0;
        var result = await new StreamReader(outputStream).ReadToEndAsync();
        Assert.Contains("\"jsonrpc\":\"2.0\"", result);
        Assert.Contains("\"method\":\"test_method\"", result);
        Assert.EndsWith("\n", result);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var inputStream = new MemoryStream();
        var outputStream = new MemoryStream();
        var transport = new StdioTransport(_mockLogger.Object, inputStream, outputStream);

        // Act & Assert
        transport.Dispose();
        transport.Dispose(); // Should not throw on double dispose
    }
}
