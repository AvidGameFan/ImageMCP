using ImageMCP.Models;
using ImageMCP.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace ImageMCP.Transport;

/// <summary>
/// HTTP transport for MCP protocol (EDMCP-style)
/// </summary>
public class HttpMcpTransport
{
    private readonly ILogger<HttpMcpTransport> _logger;
    private readonly ToolRegistry _toolRegistry;
    private readonly McpSettings _settings;

    public HttpMcpTransport(
        ILogger<HttpMcpTransport> logger,
        ToolRegistry toolRegistry,
        McpSettings settings)
    {
        _logger = logger;
        _toolRegistry = toolRegistry;
        _settings = settings;
    }

    /// <summary>
    /// Configure HTTP endpoints for MCP
    /// </summary>
    public void ConfigureEndpoints(WebApplication app)
    {
        // HTTP GET endpoint for health/info (EDMCP-style)
        app.MapGet("/mcp", async context =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                name = _settings.ServerName,
                version = _settings.ServerVersion,
                protocols = new[] { "json-rpc" }
            }));
        });

        // HTTP POST endpoint for MCP JSON-RPC requests
        app.MapPost("/mcp", HandleHttpMcp);

        // Health check endpoint
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        _logger.LogInformation("HTTP MCP endpoints configured on {Url}/mcp", _settings.HttpUrl);
    }

    private async Task HandleHttpMcp(HttpContext context)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        try
        {
            context.Request.EnableBuffering();
            var bodyText = await new StreamReader(context.Request.Body).ReadToEndAsync();

            _logger.LogDebug("HTTP MCP request received: {Body}", bodyText);

            if (string.IsNullOrWhiteSpace(bodyText))
            {
                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new McpMessage
                {
                    JsonRpc = "2.0",
                    Error = new McpError { Code = -32600, Message = "Invalid Request: empty body" }
                }, options));
                return;
            }

            McpMessage? request;
            try
            {
                request = JsonSerializer.Deserialize<McpMessage>(bodyText, options);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON parsing error");
                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new HttpJsonRpcResponse
                {
                    Error = new HttpJsonRpcError { Code = -32700, Message = "Parse error - Invalid JSON", Data = jsonEx.Message }
                }, options));
                return;
            }

            if (request == null)
            {
                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new HttpJsonRpcResponse
                {
                    Error = new HttpJsonRpcError { Code = -32600, Message = "Invalid Request" }
                }, options));
                return;
            }

            var response = await HandleMcpRequestAsync(request);
            
            // Log the response we're sending
            _logger.LogDebug("Sending response: {Response}", JsonSerializer.Serialize(response, options));
            
            context.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(context.Response.Body, response, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in HTTP MCP handler");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new McpMessage
            {
                JsonRpc = "2.0",
                Error = new McpError { Code = -32603, Message = "Internal error", Data = ex.Message }
            }));
        }
    }

    private async Task<McpMessage> HandleMcpRequestAsync(McpMessage request)
    {
        try
        {
            return request.Method switch
            {
                "initialize" => HandleInitialize(request),
                "tools/list" => HandleToolsList(request),
                "tools/call" => await HandleToolCallAsync(request),
                "ping" => HandlePing(request),
                _ => new McpMessage
                {
                    JsonRpc = "2.0",
                    Id = request.Id,
                    Error = new McpError { Code = -32601, Message = $"Method not found: {request.Method}" }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request");
            return new McpMessage
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Error = new McpError { Code = -32603, Message = "Internal error", Data = ex.Message }
            };
        }
    }

    private McpMessage HandleInitialize(McpMessage request)
    {
        _logger.LogInformation("Handling initialize request");

        return new McpMessage
        {
            JsonRpc = "2.0",
            Id = request.Id,
            Result = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { tools = new { } },
                serverInfo = new
                {
                    name = _settings.ServerName,
                    version = _settings.ServerVersion
                }
            }
        };
    }

    private McpMessage HandleToolsList(McpMessage request)
    {
        _logger.LogInformation("Handling tools/list request");

        var tools = _toolRegistry.GetTools().ToList();
        _logger.LogInformation("Returning {Count} tools", tools.Count);
        foreach (var tool in tools)
        {
            _logger.LogDebug("Tool: {Name} - {Description}", tool.Name, tool.Description);
        }
        
        return new McpMessage
        {
            JsonRpc = "2.0",
            Id = request.Id,
            Result = new { tools }
        };
    }

    private async Task<McpMessage> HandleToolCallAsync(McpMessage request)
    {
        _logger.LogInformation("Handling tools/call request");

        if (request.Params == null)
        {
            return new McpMessage
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Error = new McpError { Code = -32602, Message = "Invalid params" }
            };
        }

        var paramsJson = JsonSerializer.Serialize(request.Params);
        var callParams = JsonSerializer.Deserialize<ToolCallParams>(paramsJson);

        if (callParams == null || string.IsNullOrEmpty(callParams.Name))
        {
            return new McpMessage
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Error = new McpError { Code = -32602, Message = "Invalid tool call params" }
            };
        }

        if (!_toolRegistry.HasTool(callParams.Name))
        {
            return new McpMessage
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Error = new McpError { Code = -32602, Message = $"Tool not found: {callParams.Name}" }
            };
        }

        try
        {
            var result = await _toolRegistry.ExecuteToolAsync(callParams.Name, callParams.Arguments ?? new { });

            return new McpMessage
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Result = new { content = new[] { result } }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", callParams.Name);
            return new McpMessage
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Error = new McpError { Code = -32603, Message = $"Tool execution failed: {ex.Message}" }
            };
        }
    }

    private McpMessage HandlePing(McpMessage request)
    {
        return new McpMessage
        {
            JsonRpc = "2.0",
            Id = request.Id,
            Result = new { }
        };
    }

    private class ToolCallParams
    {
        public string Name { get; set; } = string.Empty;
        public object? Arguments { get; set; }
    }
}

