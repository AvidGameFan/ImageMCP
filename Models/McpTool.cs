using System.Text.Json.Serialization;

namespace ImageMCP.Models;

/// <summary>
/// MCP Tool definition
/// </summary>
public class McpTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("inputSchema")]
    public McpToolInputSchema InputSchema { get; set; } = new();
}

public class McpToolInputSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";
    
    [JsonPropertyName("properties")]
    public Dictionary<string, McpToolProperty> Properties { get; set; } = new();
    
    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}

public class McpToolProperty
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
