using ImageMCP.Models;
using ImageMCP.Transport;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ImageMCP.Server;

/// <summary>
/// Core MCP server implementation
/// </summary>
public class McpServer
{
    private readonly StdioTransport _transport;
    private readonly ToolRegistry _toolRegistry;
    private readonly McpSettings _settings;
    private readonly ILogger<McpServer> _logger;

    public McpServer(
        StdioTransport transport,
        ToolRegistry toolRegistry,
        McpSettings settings,
        ILogger<McpServer> logger)
    {
        _transport = transport;
        _toolRegistry = toolRegistry;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Start the MCP server and process messages
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting MCP server: {ServerName} v{Version}", 
            _settings.ServerName, _settings.ServerVersion);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var message = await _transport.ReadMessageAsync(cancellationToken);
                if (message == null)
                {
                    continue;
                }

                var response = await ProcessMessageAsync(message, cancellationToken);
                if (response != null)
                {
                    await _transport.WriteMessageAsync(response, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Server shutdown requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
            }
        }

        _logger.LogInformation("MCP server stopped");
    }

    /// <summary>
    /// Process an incoming MCP message
    /// </summary>
    private async Task<McpMessage?> ProcessMessageAsync(McpMessage message, CancellationToken cancellationToken)
    {
        if (message.Method == null)
        {
            return null;
        }

        try
        {
            return message.Method switch
            {
                "initialize" => await HandleInitializeAsync(message),
                "tools/list" => await HandleToolsListAsync(message),
                "tools/call" => await HandleToolCallAsync(message),
                "ping" => HandlePing(message),
                _ => CreateErrorResponse(message, -32601, $"Method not found: {message.Method}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling method: {Method}", message.Method);
            return CreateErrorResponse(message, -32603, $"Internal error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle initialize request
    /// </summary>
    private Task<McpMessage> HandleInitializeAsync(McpMessage message)
    {
        _logger.LogInformation("Handling initialize request");
        
        var result = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new { }
            },
            serverInfo = new
            {
                name = _settings.ServerName,
                version = _settings.ServerVersion
            }
        };

        return Task.FromResult(new McpMessage
        {
            JsonRpc = "2.0",
            Id = message.Id,
            Result = result
        });
    }

    /// <summary>
    /// Handle tools/list request
    /// </summary>
    private Task<McpMessage> HandleToolsListAsync(McpMessage message)
    {
        _logger.LogInformation("Handling tools/list request");
        
        var tools = _toolRegistry.GetTools().ToList();
        var result = new { tools };

        return Task.FromResult(new McpMessage
        {
            JsonRpc = "2.0",
            Id = message.Id,
            Result = result
        });
    }

    /// <summary>
    /// Handle tools/call request
    /// </summary>
    private async Task<McpMessage> HandleToolCallAsync(McpMessage message)
    {
        _logger.LogInformation("Handling tools/call request");

        if (message.Params == null)
        {
            return CreateErrorResponse(message, -32602, "Invalid params");
        }

        var paramsJson = JsonSerializer.Serialize(message.Params);
        var callParams = JsonSerializer.Deserialize<ToolCallParams>(paramsJson);

        if (callParams == null || string.IsNullOrEmpty(callParams.Name))
        {
            return CreateErrorResponse(message, -32602, "Invalid tool call params");
        }

        if (!_toolRegistry.HasTool(callParams.Name))
        {
            return CreateErrorResponse(message, -32602, $"Tool not found: {callParams.Name}");
        }

        try
        {
            var result = await _toolRegistry.ExecuteToolAsync(callParams.Name, callParams.Arguments ?? new { });
            
            return new McpMessage
            {
                JsonRpc = "2.0",
                Id = message.Id,
                Result = new { content = new[] { result } }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", callParams.Name);
            return CreateErrorResponse(message, -32603, $"Tool execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle ping request
    /// </summary>
    private McpMessage HandlePing(McpMessage message)
    {
        return new McpMessage
        {
            JsonRpc = "2.0",
            Id = message.Id,
            Result = new { }
        };
    }

    /// <summary>
    /// Create an error response
    /// </summary>
    private McpMessage CreateErrorResponse(McpMessage message, int code, string errorMessage)
    {
        return new McpMessage
        {
            JsonRpc = "2.0",
            Id = message.Id,
            Error = new McpError
            {
                Code = code,
                Message = errorMessage
            }
        };
    }

    private class ToolCallParams
    {
        public string Name { get; set; } = string.Empty;
        public object? Arguments { get; set; }
    }
}
