using ImageMCP.Models;
using Xunit;

namespace ImageMCP.Tests.Models;

public class ImageGenerationTests
{
    [Fact]
    public void ImageGenerationRequest_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var request = new ImageGenerationRequest();

        // Assert
        Assert.Equal(string.Empty, request.Prompt);
        Assert.Null(request.NegativePrompt);
        Assert.Null(request.Template);
    }

    [Fact]
    public void ImageGenerationRequest_ShouldAllowSettingProperties()
    {
        // Arrange & Act
        var request = new ImageGenerationRequest
        {
            Prompt = "A beautiful sunset",
            NegativePrompt = "blurry, low quality",
            Template = "./custom_template.json"
        };

        // Assert
        Assert.Equal("A beautiful sunset", request.Prompt);
        Assert.Equal("blurry, low quality", request.NegativePrompt);
        Assert.Equal("./custom_template.json", request.Template);
    }

    [Fact]
    public void ImageGenerationResponse_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var response = new ImageGenerationResponse();

        // Assert
        Assert.False(response.Success);
        Assert.Null(response.ImageUrl);
        Assert.Null(response.ImageData);
        Assert.Null(response.PromptId);
        Assert.Null(response.Error);
    }

    [Fact]
    public void ImageGenerationResponse_SuccessResponse()
    {
        // Arrange & Act
        var response = new ImageGenerationResponse
        {
            Success = true,
            ImageUrl = "http://localhost:8188/output/image.png",
            PromptId = "12345"
        };

        // Assert
        Assert.True(response.Success);
        Assert.Equal("http://localhost:8188/output/image.png", response.ImageUrl);
        Assert.Equal("12345", response.PromptId);
        Assert.Null(response.Error);
    }

    [Fact]
    public void ImageGenerationResponse_ErrorResponse()
    {
        // Arrange & Act
        var response = new ImageGenerationResponse
        {
            Success = false,
            Error = "ComfyUI connection failed"
        };

        // Assert
        Assert.False(response.Success);
        Assert.Equal("ComfyUI connection failed", response.Error);
        Assert.Null(response.ImageUrl);
    }
}
