namespace ImageMCP.Models;

/// <summary>
/// Configuration settings for MCP server
/// </summary>
public class McpSettings
{
    public string ServerName { get; set; } = "ImageMCP"; //"ComfyUI Image Generator";
    public string ServerVersion { get; set; } = "1.0.0";
    
    /// <summary>
    /// Server mode: "Auto" (detect), "Stdio" (MCP standard), or "Http" (EDMCP-style)
    /// </summary>
    public string Mode { get; set; } = "Auto";
    
    /// <summary>
    /// HTTP server port (when Mode is Http or Auto with no stdin)
    /// </summary>
    public int HttpPort { get; set; } = 5243;
    
    /// <summary>
    /// HTTP server URL
    /// </summary>
    public string HttpUrl { get; set; } = "http://localhost:5243";
}
