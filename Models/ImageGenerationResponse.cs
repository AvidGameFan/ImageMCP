using System.Text.Json.Serialization;

namespace ImageMCP.Models;

/// <summary>
/// Response containing generated image information
/// </summary>
public class ImageGenerationResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
    
    [JsonPropertyName("image_data")]
    public string? ImageData { get; set; }
    
    [JsonPropertyName("prompt_id")]
    public string? PromptId { get; set; }
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
