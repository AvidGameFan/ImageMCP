using ImageMCP.Server;
using ImageMCP.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ImageMCP.Tests.Server;

public class ToolRegistryTests
{
    private readonly Mock<ILogger<ToolRegistry>> _mockLogger;
    private readonly ToolRegistry _toolRegistry;

    public ToolRegistryTests()
    {
        _mockLogger = new Mock<ILogger<ToolRegistry>>();
        _toolRegistry = new ToolRegistry(_mockLogger.Object);
    }

    [Fact]
    public void RegisterTool_ShouldAddToolToRegistry()
    {
        // Arrange
        var tool = new McpTool
        {
            Name = "test_tool",
            Description = "A test tool"
        };
        Func<object, Task<object>> handler = async (param) => await Task.FromResult("result");

        // Act
        _toolRegistry.RegisterTool(tool, handler);

        // Assert
        Assert.True(_toolRegistry.HasTool("test_tool"));
        var tools = _toolRegistry.GetTools().ToList();
        Assert.Single(tools);
        Assert.Equal("test_tool", tools[0].Name);
    }

    [Fact]
    public async Task ExecuteToolAsync_ShouldCallHandler()
    {
        // Arrange
        var tool = new McpTool { Name = "test_tool" };
        var handlerCalled = false;
        Func<object, Task<object>> handler = async (param) =>
        {
            handlerCalled = true;
            return await Task.FromResult("result");
        };
        _toolRegistry.RegisterTool(tool, handler);

        // Act
        var result = await _toolRegistry.ExecuteToolAsync("test_tool", new { });

        // Assert
        Assert.True(handlerCalled);
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task ExecuteToolAsync_ShouldThrowForUnknownTool()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _toolRegistry.ExecuteToolAsync("unknown_tool", new { }));
    }

    [Fact]
    public void GetTools_ShouldReturnAllRegisteredTools()
    {
        // Arrange
        var tool1 = new McpTool { Name = "tool1" };
        var tool2 = new McpTool { Name = "tool2" };
        Func<object, Task<object>> handler = async (p) => await Task.FromResult("result");

        // Act
        _toolRegistry.RegisterTool(tool1, handler);
        _toolRegistry.RegisterTool(tool2, handler);
        var tools = _toolRegistry.GetTools().ToList();

        // Assert
        Assert.Equal(2, tools.Count);
        Assert.Contains(tools, t => t.Name == "tool1");
        Assert.Contains(tools, t => t.Name == "tool2");
    }

    [Fact]
    public void HasTool_ShouldReturnFalseForUnregisteredTool()
    {
        // Assert
        Assert.False(_toolRegistry.HasTool("nonexistent_tool"));
    }
}
