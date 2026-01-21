using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageMCP.Models;

/// <summary>
/// Represents a ComfyUI workflow definition
/// </summary>
public class ComfyWorkflow
{
    [JsonPropertyName("nodes")]
    public List<WorkflowNode> Nodes { get; set; } = new();

    [JsonPropertyName("links")]
    public JsonElement? Links { get; set; }

    [JsonPropertyName("groups")]
    public JsonElement? Groups { get; set; }

    [JsonPropertyName("config")]
    public JsonElement? Config { get; set; }

    [JsonPropertyName("extra")]
    public JsonElement? Extra { get; set; }

    [JsonPropertyName("version")]
    public JsonElement? Version { get; set; }

    [JsonPropertyName("last_node_id")]
    public int? LastNodeId { get; set; }

    [JsonPropertyName("last_link_id")]
    public int? LastLinkId { get; set; }
}

/// <summary>
/// Represents a single node in a ComfyUI workflow
/// </summary>
public class WorkflowNode
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("pos")]
    public JsonElement? Pos { get; set; }

    [JsonPropertyName("size")]
    public JsonElement? Size { get; set; }

    [JsonPropertyName("flags")]
    public JsonElement? Flags { get; set; }

    [JsonPropertyName("order")]
    public int? Order { get; set; }

    [JsonPropertyName("mode")]
    public int? Mode { get; set; }

    [JsonPropertyName("inputs")]
    public List<NodeInput>? Inputs { get; set; }

    [JsonPropertyName("outputs")]
    public JsonElement? Outputs { get; set; }

    [JsonPropertyName("properties")]
    public JsonElement? Properties { get; set; }

    [JsonPropertyName("widgets_values")]
    public JsonElement? WidgetsValues { get; set; }
}

/// <summary>
/// Represents an input to a workflow node
/// </summary>
public class NodeInput
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("link")]
    public JsonElement? Link { get; set; }

    [JsonPropertyName("widget")]
    public JsonElement? Widget { get; set; }

    [JsonPropertyName("localized_name")]
    public string? LocalizedName { get; set; }
}
