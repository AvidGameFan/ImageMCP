using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageMCP.Models;

/// <summary>
/// Request to submit a workflow prompt to ComfyUI
/// </summary>
public class ComfyPromptRequest
{
    [JsonPropertyName("prompt")]
    public Dictionary<string, ComfyNodeData> Prompt { get; set; } = new();

    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;
}

/// <summary>
/// Node data in the ComfyUI prompt format
/// </summary>
public class ComfyNodeData
{
    [JsonPropertyName("inputs")]
    public Dictionary<string, object> Inputs { get; set; } = new();

    [JsonPropertyName("class_type")]
    public string ClassType { get; set; } = string.Empty;

    [JsonPropertyName("_meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Meta { get; set; }
}

/// <summary>
/// Response from ComfyUI prompt submission
/// </summary>
public class ComfyPromptResponse
{
    [JsonPropertyName("prompt_id")]
    public string PromptId { get; set; } = string.Empty;

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("node_errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? NodeErrors { get; set; }
}

/// <summary>
/// WebSocket message from ComfyUI
/// </summary>
public class ComfyWebSocketMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}

/// <summary>
/// History response from ComfyUI
/// </summary>
public class ComfyHistoryResponse
{
    [JsonPropertyName("outputs")]
    public Dictionary<string, ComfyNodeOutput>? Outputs { get; set; }

    [JsonPropertyName("status")]
    public JsonElement? Status { get; set; }
}

/// <summary>
/// Output from a ComfyUI node
/// </summary>
public class ComfyNodeOutput
{
    [JsonPropertyName("images")]
    public List<ComfyImageInfo>? Images { get; set; }
}

/// <summary>
/// Information about a generated image
/// </summary>
public class ComfyImageInfo
{
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("subfolder")]
    public string Subfolder { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}
