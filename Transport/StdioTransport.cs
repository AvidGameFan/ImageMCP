using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using ImageMCP.Models;

namespace ImageMCP.Transport;

/// <summary>
/// Handles stdio communication for MCP protocol
/// </summary>
public class StdioTransport : IDisposable
{
    private readonly ILogger<StdioTransport> _logger;
    private readonly Stream _inputStream;
    private readonly Stream _outputStream;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;

    public StdioTransport(ILogger<StdioTransport> logger)
        : this(logger, Console.OpenStandardInput(), Console.OpenStandardOutput())
    {
    }

    public StdioTransport(ILogger<StdioTransport> logger, Stream inputStream, Stream outputStream)
    {
        _logger = logger;
        _inputStream = inputStream;
        _outputStream = outputStream;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// Read a message from stdin
    /// </summary>
    public async Task<McpMessage?> ReadMessageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(_inputStream, Encoding.UTF8, leaveOpen: true);
            var line = await reader.ReadLineAsync(cancellationToken);
            
            // If we get null (EOF), wait a bit before trying again
            // This prevents tight loops when stdin is redirected
            if (line == null)
            {
                _logger.LogDebug("Received EOF on stdin, waiting...");
                await Task.Delay(100, cancellationToken);
                return null;
            }
            
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            _logger.LogDebug("Received: {Message}", line);
            return JsonSerializer.Deserialize<McpMessage>(line);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading message from stdin");
            return null;
        }
    }

    /// <summary>
    /// Write a message to stdout
    /// </summary>
    public async Task WriteMessageAsync(McpMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            _logger.LogDebug("Sending: {Message}", json);
            
            var bytes = Encoding.UTF8.GetBytes(json + "\n");
            await _outputStream.WriteAsync(bytes, cancellationToken);
            await _outputStream.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing message to stdout");
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _disposed = true;
        }
    }
}
