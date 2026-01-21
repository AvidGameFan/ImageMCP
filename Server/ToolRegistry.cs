using ImageMCP.Models;
using Microsoft.Extensions.Logging;

namespace ImageMCP.Server;

/// <summary>
/// Registry for MCP tools
/// </summary>
public class ToolRegistry
{
    private readonly Dictionary<string, McpTool> _tools = new();
    private readonly Dictionary<string, Func<object, Task<object>>> _handlers = new();
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(ILogger<ToolRegistry> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a tool with its handler
    /// </summary>
    public void RegisterTool(McpTool tool, Func<object, Task<object>> handler)
    {
        _tools[tool.Name] = tool;
        _handlers[tool.Name] = handler;
        _logger.LogInformation("Registered tool: {ToolName}", tool.Name);
    }

    /// <summary>
    /// Get all registered tools
    /// </summary>
    public IEnumerable<McpTool> GetTools()
    {
        return _tools.Values;
    }

    /// <summary>
    /// Execute a tool by name
    /// </summary>
    public async Task<object> ExecuteToolAsync(string toolName, object parameters)
    {
        if (!_handlers.TryGetValue(toolName, out var handler))
        {
            throw new InvalidOperationException($"Tool not found: {toolName}");
        }

        _logger.LogInformation("Executing tool: {ToolName}", toolName);
        return await handler(parameters);
    }

    /// <summary>
    /// Check if a tool is registered
    /// </summary>
    public bool HasTool(string toolName)
    {
        return _tools.ContainsKey(toolName);
    }
}
