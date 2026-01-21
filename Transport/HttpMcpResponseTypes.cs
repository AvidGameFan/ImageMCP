using System.Text.Json.Serialization;

namespace ImageMCP.Transport;

/// <summary>
/// HTTP MCP response classes (EDMCP-compatible)
/// These are specifically for HTTP mode to ensure compatibility with LM Studio
/// </summary>

public class HttpJsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; set; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HttpJsonRpcError? Error { get; set; }
}

public class HttpJsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }
}

public class HttpInitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2024-11-05";

    [JsonPropertyName("capabilities")]
    public object Capabilities { get; set; } = new { tools = new { } };

    [JsonPropertyName("serverInfo")]
    public HttpServerInfo ServerInfo { get; set; } = new();
}

public class HttpServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

public class HttpToolListResult
{
    [JsonPropertyName("tools")]
    public List<object> Tools { get; set; } = new();
}

public class HttpToolResult
{
    [JsonPropertyName("content")]
    public List<object> Content { get; set; } = new();

    [JsonPropertyName("isError")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsError { get; set; } = false;
}
