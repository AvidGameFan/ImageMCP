namespace ImageMCP.Models;

/// <summary>
/// Configuration settings for ComfyUI integration
/// </summary>
public class ComfyUISettings
{
    public string ApiEndpoint { get; set; } = "ws://127.0.0.1:8188";
    public string DefaultTemplate { get; set; } = "workflows/default_workflow.json";
    public int TimeoutSeconds { get; set; } = 300;
    public int PollIntervalSeconds { get; set; } = 1;
}
