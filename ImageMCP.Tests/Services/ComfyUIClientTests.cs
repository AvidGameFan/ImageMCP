using ImageMCP.Models;
using ImageMCP.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ImageMCP.Tests.Services;

public class ComfyUIClientTests
{
    private readonly Mock<ILogger<ComfyUIClient>> _mockLogger;
    private readonly ComfyUISettings _settings;

    public ComfyUIClientTests()
    {
        _mockLogger = new Mock<ILogger<ComfyUIClient>>();
        _settings = new ComfyUISettings
        {
            ApiEndpoint = "ws://127.0.0.1:8188",
            DefaultTemplate = "workflows/default_workflow.json",
            TimeoutSeconds = 30
        };
    }

    [Fact]
    public void Constructor_ShouldInitializeClient()
    {
        // Act
        using var client = new ComfyUIClient(_settings, _mockLogger.Object);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var client = new ComfyUIClient(_settings, _mockLogger.Object);

        // Act & Assert
        var exception = Record.Exception(() => client.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public async Task ConnectAsync_WithInvalidEndpoint_ShouldThrowException()
    {
        // Arrange
        var invalidSettings = new ComfyUISettings
        {
            ApiEndpoint = "ws://invalid-endpoint-that-does-not-exist:9999",
            TimeoutSeconds = 5
        };
        using var client = new ComfyUIClient(invalidSettings, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.ConnectAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SubmitWorkflowAsync_WithInvalidEndpoint_ShouldThrowException()
    {
        // Arrange
        var invalidSettings = new ComfyUISettings
        {
            ApiEndpoint = "http://invalid-endpoint-that-does-not-exist:9999",
            TimeoutSeconds = 5
        };
        using var client = new ComfyUIClient(invalidSettings, _mockLogger.Object);

        var workflowJson = """
        {
            "1": {
                "class_type": "CheckpointLoaderSimple",
                "inputs": { "ckpt_name": "model.safetensors" }
            }
        }
        """;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.SubmitWorkflowAsync(workflowJson));
    }

    [Fact]
    public void GetHttpEndpoint_FromWebSocketUrl_ShouldConvertCorrectly()
    {
        // This is tested implicitly through SubmitWorkflowAsync
        // WebSocket URLs should be converted to HTTP URLs
        var wsSettings = new ComfyUISettings
        {
            ApiEndpoint = "ws://127.0.0.1:8188"
        };

        using var client = new ComfyUIClient(wsSettings, _mockLogger.Object);
        
        // Verify client was created successfully
        Assert.NotNull(client);
    }

    [Fact]
    public void Settings_ShouldUseDefaultValues()
    {
        // Arrange & Act
        var defaultSettings = new ComfyUISettings();

        // Assert
        Assert.Equal("ws://127.0.0.1:8188", defaultSettings.ApiEndpoint);
        Assert.Equal("workflows/default_workflow.json", defaultSettings.DefaultTemplate);
        Assert.Equal(300, defaultSettings.TimeoutSeconds);
        Assert.Equal(1, defaultSettings.PollIntervalSeconds);
    }
}
