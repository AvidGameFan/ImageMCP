using ImageMCP.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace ImageMCP.Tests.Services;

public class WorkflowTemplateManagerTests
{
    private readonly Mock<ILogger<WorkflowTemplateManager>> _mockLogger;
    private readonly WorkflowTemplateManager _manager;

    public WorkflowTemplateManagerTests()
    {
        _mockLogger = new Mock<ILogger<WorkflowTemplateManager>>();
        _manager = new WorkflowTemplateManager(_mockLogger.Object);
    }

    [Fact]
    public async Task LoadTemplateAsync_WithValidFile_ShouldReturnJsonDocument()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var json = @"{""nodes"": [], ""links"": []}";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var result = await _manager.LoadTemplateAsync(tempFile);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("nodes", out _));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadTemplateAsync_WithMissingFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = "nonexistent_workflow.json";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _manager.LoadTemplateAsync(nonExistentFile));
    }

    [Fact]
    public void InjectPrompts_ShouldReplacePositivePrompt()
    {
        // Arrange
        var templateJson = @"{
            ""nodes"": [
                {
                    ""id"": 1,
                    ""type"": ""CLIPTextEncode"",
                    ""widgets_values"": [""original positive prompt""]
                },
                {
                    ""id"": 2,
                    ""type"": ""CLIPTextEncode"",
                    ""widgets_values"": [""worst quality, low quality""]
                }
            ]
        }";
        var template = JsonDocument.Parse(templateJson);
        var newPrompt = "a beautiful landscape";

        // Act
        var result = _manager.InjectPrompts(template, newPrompt, null);

        // Assert
        var nodes = result.RootElement.GetProperty("nodes");
        var firstNode = nodes[0];
        var widgetValues = firstNode.GetProperty("widgets_values");
        var promptValue = widgetValues[0].GetString();

        Assert.Equal(newPrompt, promptValue);
    }

    [Fact]
    public void InjectPrompts_ShouldReplaceNegativePrompt()
    {
        // Arrange
        var templateJson = @"{
            ""nodes"": [
                {
                    ""id"": 1,
                    ""type"": ""CLIPTextEncode"",
                    ""widgets_values"": [""positive prompt""]
                },
                {
                    ""id"": 2,
                    ""type"": ""CLIPTextEncode"",
                    ""widgets_values"": [""worst quality, low quality""]
                }
            ]
        }";
        var template = JsonDocument.Parse(templateJson);
        var positivePrompt = "a beautiful landscape";
        var negativePrompt = "blurry, ugly";

        // Act
        var result = _manager.InjectPrompts(template, positivePrompt, negativePrompt);

        // Assert
        var nodes = result.RootElement.GetProperty("nodes");
        var secondNode = nodes[1];
        var widgetValues = secondNode.GetProperty("widgets_values");
        var promptValue = widgetValues[0].GetString();

        Assert.Equal(negativePrompt, promptValue);
    }

    [Fact]
    public void InjectPrompts_WithOnlyPositivePrompt_ShouldNotReplaceNegative()
    {
        // Arrange
        var templateJson = @"{
            ""nodes"": [
                {
                    ""id"": 1,
                    ""type"": ""CLIPTextEncode"",
                    ""widgets_values"": [""positive prompt""]
                },
                {
                    ""id"": 2,
                    ""type"": ""CLIPTextEncode"",
                    ""widgets_values"": [""worst quality""]
                }
            ]
        }";
        var template = JsonDocument.Parse(templateJson);
        var newPrompt = "a beautiful landscape";

        // Act
        var result = _manager.InjectPrompts(template, newPrompt, null);

        // Assert
        var nodes = result.RootElement.GetProperty("nodes");
        var secondNode = nodes[1];
        var widgetValues = secondNode.GetProperty("widgets_values");
        var promptValue = widgetValues[0].GetString();

        Assert.Equal("worst quality", promptValue);
    }

    [Fact]
    public void ConvertToApiFormat_WithBasicWorkflow_ShouldConvertSuccessfully()
    {
        // Arrange
        var workflowJson = @"{
            ""nodes"": [
                {
                    ""id"": 1,
                    ""type"": ""CheckpointLoaderSimple"",
                    ""inputs"": [
                        {
                            ""name"": ""ckpt_name"",
                            ""type"": ""STRING"",
                            ""widget"": {}
                        }
                    ],
                    ""widgets_values"": [""model.safetensors""]
                }
            ],
            ""links"": []
        }";
        var workflow = JsonDocument.Parse(workflowJson);

        // Act
        var result = _manager.ConvertToApiFormat(workflow);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("1"));
        Assert.Equal("CheckpointLoaderSimple", result["1"].ClassType);
    }

    [Fact]
    public void ConvertToApiFormat_WithoutNodes_ShouldThrowException()
    {
        // Arrange
        var workflowJson = @"{""links"": []}";
        var workflow = JsonDocument.Parse(workflowJson);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _manager.ConvertToApiFormat(workflow));
    }

    [Fact]
    public void InjectPrompts_WithMultipleCLIPNodes_ShouldOnlyReplaceFirstOfEachType()
    {
        // Arrange
        var templateJson = @"{
            ""nodes"": [
                {
                    ""id"": 1,
                    ""type"": ""CLIPTextEncode"",
                    ""widgets_values"": [""first positive""]
                },
                {
                    ""id"": 2,
                    ""type"": ""CLIPTextEncode"",
                    ""widgets_values"": [""second positive""]
                },
                {
                    ""id"": 3,
                    ""type"": ""CLIPTextEncode"",
                    ""widgets_values"": [""worst quality""]
                },
                {
                    ""id"": 4,
                    ""type"": ""CLIPTextEncode"",
                    ""widgets_values"": [""another worst quality""]
                }
            ]
        }";
        var template = JsonDocument.Parse(templateJson);
        var positivePrompt = "new positive prompt";
        var negativePrompt = "new negative prompt";

        // Act
        var result = _manager.InjectPrompts(template, positivePrompt, negativePrompt);

        // Assert
        var nodes = result.RootElement.GetProperty("nodes");
        
        // First positive should be replaced
        Assert.Equal(positivePrompt, nodes[0].GetProperty("widgets_values")[0].GetString());
        
        // Second positive should remain unchanged
        Assert.Equal("second positive", nodes[1].GetProperty("widgets_values")[0].GetString());
        
        // First negative should be replaced
        Assert.Equal(negativePrompt, nodes[2].GetProperty("widgets_values")[0].GetString());
        
        // Second negative should remain unchanged
        Assert.Equal("another worst quality", nodes[3].GetProperty("widgets_values")[0].GetString());
    }
}
