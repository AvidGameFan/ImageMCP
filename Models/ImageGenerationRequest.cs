using System.Text.Json.Serialization;

namespace ImageMCP.Models;

/// <summary>
/// Request to generate an image
/// </summary>
public class ImageGenerationRequest
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;
    
    [JsonPropertyName("negative_prompt")]
    public string? NegativePrompt { get; set; }
    
    [JsonPropertyName("template")]
    public string? Template { get; set; }
}
