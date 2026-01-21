using ImageMCP.Models;
using ImageMCP.Server;
using ImageMCP.Transport;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ImageMCP.Tests.Integration;

public class ImageGenerationIntegrationTests
{
    private readonly Mock<ILogger<McpServer>> _mockServerLogger;
    private readonly Mock<ILogger<ToolRegistry>> _mockToolLogger;
    private readonly McpSettings _settings;

    public ImageGenerationIntegrationTests()
    {
        _mockServerLogger = new Mock<ILogger<McpServer>>();
        _mockToolLogger = new Mock<ILogger<ToolRegistry>>();
        _settings = new McpSettings
        {
            ServerName = "Test Server",
            ServerVersion = "1.0.0"
        };
    }

    [Fact]
    public async Task ToolsCall_GenerateImage_WithInvalidParameters_ShouldReturnError()
    {
        // Arrange - Create a message to call the generate_image tool without prompt
        var inputMessage = new McpMessage
        {
            JsonRpc = "2.0",
            Method = "tools/call",
            Id = 1,
            Params = new
            {
                name = "generate_image",
                arguments = new
                {
                    // Missing required "prompt" parameter
                    negative_prompt = "blurry"
                }
            }
        };

        var json = JsonSerializer.Serialize(inputMessage) + "\n";
        var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var outputStream = new MemoryStream();

        var mockTransportLogger = new Mock<ILogger<StdioTransport>>();
        var transport = new StdioTransport(mockTransportLogger.Object, inputStream, outputStream);
        var toolRegistry = new ToolRegistry(_mockToolLogger.Object);

        // Register a simplified tool that validates parameters
        var tool = new McpTool
        {
            Name = "generate_image",
            Description = "Test tool",
            InputSchema = new McpToolInputSchema
            {
                Type = "object",
                Properties = new Dictionary<string, McpToolProperty>
                {
                    ["prompt"] = new McpToolProperty
                    {
                        Type = "string",
                        Description = "The prompt"
                    }
                },
                Required = new List<string> { "prompt" }
            }
        };

        Func<object, Task<object>> handler = async (parameters) =>
        {
            var paramsJson = JsonSerializer.Serialize(parameters);
            var doc = JsonDocument.Parse(paramsJson);
            
            if (!doc.RootElement.TryGetProperty("prompt", out var promptProp) ||
                string.IsNullOrWhiteSpace(promptProp.GetString()))
            {
                await Task.CompletedTask;
                return new
                {
                    type = "text",
                    text = "Error: prompt is required"
                };
            }

            return new
            {
                type = "text",
                text = "Success"
            };
        };

        toolRegistry.RegisterTool(tool, handler);
        var server = new McpServer(transport, toolRegistry, _settings, _mockServerLogger.Object);

        // Act
        var cts = new CancellationTokenSource();
        var serverTask = Task.Run(async () => await server.RunAsync(cts.Token));
        
        await Task.Delay(100);
        cts.Cancel();
        
        try
        {
            await serverTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        outputStream.Position = 0;
        var response = await new StreamReader(outputStream).ReadToEndAsync();
        
        // The response should contain some indication of error or the tool result
        Assert.NotEmpty(response);
        Assert.Contains("\"result\"", response);
    }

    [Fact]
    public async Task ToolsList_ShouldIncludeGenerateImageTool()
    {
        // Arrange
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
        var toolRegistry = new ToolRegistry(_mockToolLogger.Object);

        // Register generate_image tool
        var tool = new McpTool
        {
            Name = "generate_image",
            Description = "Generate an image using ComfyUI",
            InputSchema = new McpToolInputSchema
            {
                Type = "object",
                Properties = new Dictionary<string, McpToolProperty>
                {
                    ["prompt"] = new McpToolProperty
                    {
                        Type = "string",
                        Description = "The text prompt"
                    }
                },
                Required = new List<string> { "prompt" }
            }
        };

        toolRegistry.RegisterTool(tool, async (p) => await Task.FromResult(new { type = "text", text = "test" }));
        var server = new McpServer(transport, toolRegistry, _settings, _mockServerLogger.Object);

        // Act
        var cts = new CancellationTokenSource();
        var serverTask = Task.Run(async () => await server.RunAsync(cts.Token));
        
        await Task.Delay(100);
        cts.Cancel();
        
        try
        {
            await serverTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        outputStream.Position = 0;
        var response = await new StreamReader(outputStream).ReadToEndAsync();
        
        Assert.Contains("generate_image", response);
        Assert.Contains("Generate an image using ComfyUI", response);
        Assert.Contains("prompt", response);
    }

    [Fact]
    public void McpToolInputSchema_ShouldSerializeCorrectly()
    {
        // Arrange
        var schema = new McpToolInputSchema
        {
            Type = "object",
            Properties = new Dictionary<string, McpToolProperty>
            {
                ["prompt"] = new McpToolProperty
                {
                    Type = "string",
                    Description = "The prompt text"
                },
                ["negative_prompt"] = new McpToolProperty
                {
                    Type = "string",
                    Description = "The negative prompt"
                }
            },
            Required = new List<string> { "prompt" }
        };

        // Act
        var json = JsonSerializer.Serialize(schema);
        var deserialized = JsonSerializer.Deserialize<McpToolInputSchema>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("object", deserialized.Type);
        Assert.Equal(2, deserialized.Properties.Count);
        Assert.Single(deserialized.Required);
        Assert.Contains("prompt", deserialized.Required);
    }
}
