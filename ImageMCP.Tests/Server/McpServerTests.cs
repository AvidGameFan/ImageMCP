using ImageMCP.Server;
using ImageMCP.Transport;
using ImageMCP.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ImageMCP.Tests.Server;

public class McpServerTests
{
    private readonly Mock<ILogger<McpServer>> _mockLogger;
    private readonly Mock<ILogger<ToolRegistry>> _mockToolLogger;
    private readonly McpSettings _settings;

    public McpServerTests()
    {
        _mockLogger = new Mock<ILogger<McpServer>>();
        _mockToolLogger = new Mock<ILogger<ToolRegistry>>();
        _settings = new McpSettings
        {
            ServerName = "Test Server",
            ServerVersion = "1.0.0"
        };
    }

    [Fact]
    public async Task HandleInitialize_ShouldReturnServerInfo()
    {
        // Arrange
        var inputMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "initialize",
            Id = 1
        };
        var json = JsonSerializer.Serialize(inputMessage) + "\n";
        var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var outputStream = new MemoryStream();

        var mockTransportLogger = new Mock<ILogger<StdioTransport>>();
        var transport = new StdioTransport(mockTransportLogger.Object, inputStream, outputStream);
        var toolRegistry = new ToolRegistry(_mockToolLogger.Object);
        var server = new McpServer(transport, toolRegistry, _settings, _mockLogger.Object);

        // Act
        var cts = new CancellationTokenSource();
        var serverTask = Task.Run(async () => await server.RunAsync(cts.Token));
        
        // Give server time to process
        await Task.Delay(100);
        cts.Cancel();
        await serverTask;

        // Assert
        outputStream.Position = 0;
        var response = await new StreamReader(outputStream).ReadToEndAsync();
        Assert.Contains("Test Server", response);
        Assert.Contains("1.0.0", response);
        Assert.Contains("protocolVersion", response);
    }

    [Fact]
    public async Task HandleToolsList_ShouldReturnRegisteredTools()
    {
        // Arrange
        var toolRegistry = new ToolRegistry(_mockToolLogger.Object);
        var testTool = new McpTool { Name = "test_tool", Description = "Test" };
        toolRegistry.RegisterTool(testTool, async (p) => await Task.FromResult("result"));

        var inputMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "tools/list",
            Id = 1
        };
        var json = JsonSerializer.Serialize(inputMessage) + "\n";
        var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var outputStream = new MemoryStream();

        var mockTransportLogger = new Mock<ILogger<StdioTransport>>();
        var transport = new StdioTransport(mockTransportLogger.Object, inputStream, outputStream);
        var server = new McpServer(transport, toolRegistry, _settings, _mockLogger.Object);

        // Act
        var cts = new CancellationTokenSource();
        var serverTask = Task.Run(async () => await server.RunAsync(cts.Token));
        
        await Task.Delay(100);
        cts.Cancel();
        await serverTask;

        // Assert
        outputStream.Position = 0;
        var response = await new StreamReader(outputStream).ReadToEndAsync();
        Assert.Contains("test_tool", response);
        Assert.Contains("tools", response);
    }

    [Fact]
    public async Task HandlePing_ShouldReturnSuccess()
    {
        // Arrange
        var inputMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "ping",
            Id = 1
        };
        var json = JsonSerializer.Serialize(inputMessage) + "\n";
        var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var outputStream = new MemoryStream();

        var mockTransportLogger = new Mock<ILogger<StdioTransport>>();
        var transport = new StdioTransport(mockTransportLogger.Object, inputStream, outputStream);
        var toolRegistry = new ToolRegistry(_mockToolLogger.Object);
        var server = new McpServer(transport, toolRegistry, _settings, _mockLogger.Object);

        // Act
        var cts = new CancellationTokenSource();
        var serverTask = Task.Run(async () => await server.RunAsync(cts.Token));
        
        await Task.Delay(100);
        cts.Cancel();
        await serverTask;

        // Assert
        outputStream.Position = 0;
        var response = await new StreamReader(outputStream).ReadToEndAsync();
        Assert.Contains("\"result\"", response);
    }
}
