using ImageMCP.Models;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ImageMCP.Services;

/// <summary>
/// Client for interacting with ComfyUI API
/// </summary>
public class ComfyUIClient : IDisposable
{
    private readonly ILogger<ComfyUIClient> _logger;
    private readonly ComfyUISettings _settings;
    private readonly HttpClient _httpClient;
    private ClientWebSocket? _webSocket;
    private readonly string _clientId;
    private bool _disposed;

    public ComfyUIClient(ComfyUISettings settings, ILogger<ComfyUIClient> logger)
    {
        _settings = settings;
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds) };
        _clientId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Connect to ComfyUI WebSocket
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            _logger.LogDebug("WebSocket already connected");
            return;
        }

        _webSocket?.Dispose();
        _webSocket = new ClientWebSocket();

        var wsEndpoint = GetWebSocketEndpoint();
        _logger.LogInformation("Connecting to ComfyUI WebSocket: {Endpoint}", wsEndpoint);

        try
        {
            await _webSocket.ConnectAsync(new Uri(wsEndpoint), cancellationToken);
            _logger.LogInformation("Connected to ComfyUI WebSocket with client ID: {ClientId}", _clientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to ComfyUI WebSocket");
            throw new InvalidOperationException($"Could not connect to ComfyUI at {wsEndpoint}", ex);
        }
    }

    /// <summary>
    /// Submit a workflow to ComfyUI
    /// </summary>
    public async Task<string> SubmitWorkflowAsync(
        string workflowJson, 
        CancellationToken cancellationToken = default)
    {
        var httpEndpoint = GetHttpEndpoint();
        var url = $"{httpEndpoint}/prompt";

        // Build the request with the workflow JSON and client ID
        var requestJson = $$"""
        {
            "prompt": {{workflowJson}},
            "client_id": "{{_clientId}}"
        }
        """;

        _logger.LogDebug("Submitting workflow to ComfyUI: {Url}", url);

        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ComfyUI returned error: {StatusCode} - {Response}", 
                    response.StatusCode, responseText);
                throw new InvalidOperationException(
                    $"ComfyUI returned HTTP {response.StatusCode}: {responseText}");
            }

            var promptResponse = JsonSerializer.Deserialize<ComfyPromptResponse>(responseText);
            
            if (promptResponse == null || string.IsNullOrEmpty(promptResponse.PromptId))
            {
                throw new InvalidOperationException("Invalid response from ComfyUI: missing prompt_id");
            }

            _logger.LogInformation("Workflow submitted successfully. Prompt ID: {PromptId}", 
                promptResponse.PromptId);

            return promptResponse.PromptId;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error submitting workflow to ComfyUI");
            throw new InvalidOperationException("Failed to submit workflow to ComfyUI", ex);
        }
    }

    /// <summary>
    /// Wait for workflow completion via WebSocket
    /// </summary>
    public async Task<bool> WaitForCompletionAsync(
        string promptId, 
        CancellationToken cancellationToken = default)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            await ConnectAsync(cancellationToken);
        }

        _logger.LogInformation("Waiting for prompt completion: {PromptId}", promptId);

        var buffer = new byte[1024 * 4];
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        try
        {
            while (_webSocket!.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                // Check timeout
                if (DateTime.UtcNow - startTime > timeout)
                {
                    throw new TimeoutException(
                        $"Workflow execution timed out after {timeout.TotalSeconds} seconds");
                }

                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), 
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogWarning("WebSocket closed by server");
                    return false;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var messageText = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = JsonSerializer.Deserialize<ComfyWebSocketMessage>(messageText);

                    if (message != null)
                    {
                        _logger.LogDebug("Received WebSocket message: {Type}", message.Type);

                        // Check for execution complete
                        if (message.Type == "executed" || message.Type == "execution_cached")
                        {
                            if (message.Data.HasValue && 
                                message.Data.Value.TryGetProperty("prompt_id", out var msgPromptId))
                            {
                                if (msgPromptId.GetString() == promptId)
                                {
                                    _logger.LogInformation("Workflow execution completed: {PromptId}", promptId);
                                    return true;
                                }
                            }
                        }

                        // Check for errors
                        if (message.Type == "execution_error")
                        {
                            var errorMsg = message.Data?.GetRawText() ?? "Unknown error";
                            _logger.LogError("Workflow execution failed: {Error}", errorMsg);
                            throw new InvalidOperationException($"Workflow execution failed: {errorMsg}");
                        }

                        // Log progress
                        if (message.Type == "progress" && message.Data.HasValue)
                        {
                            if (message.Data.Value.TryGetProperty("value", out var value) &&
                                message.Data.Value.TryGetProperty("max", out var max))
                            {
                                _logger.LogDebug("Progress: {Value}/{Max}", value.GetInt32(), max.GetInt32());
                            }
                        }
                    }
                }
            }

            return false;
        }
        catch (Exception ex) when (ex is not TimeoutException && ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error waiting for workflow completion");
            throw new InvalidOperationException("Error monitoring workflow execution", ex);
        }
    }

    /// <summary>
    /// Get generated images from ComfyUI
    /// </summary>
    public async Task<List<byte[]>> GetImagesAsync(
        string promptId, 
        CancellationToken cancellationToken = default)
    {
        var httpEndpoint = GetHttpEndpoint();
        var url = $"{httpEndpoint}/history/{promptId}";

        _logger.LogDebug("Fetching images from history: {Url}", url);

        // Give ComfyUI a moment to finalize history after execution completes
        await Task.Delay(1000, cancellationToken);

        // Retry logic - sometimes history takes a moment to be available
        const int maxRetries = 10;
        const int retryDelayMs = 1000;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    _logger.LogInformation("Retry attempt {Attempt}/{MaxRetries} for history: {PromptId}", attempt + 1, maxRetries, promptId);
                    await Task.Delay(retryDelayMs, cancellationToken);
                }

                var response = await _httpClient.GetAsync(url, cancellationToken);
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Failed to get history: HTTP {response.StatusCode}");
                }

                var history = JsonSerializer.Deserialize<Dictionary<string, ComfyHistoryResponse>>(responseText);
                
                if (history == null || !history.ContainsKey(promptId))
                {
                    if (attempt < maxRetries - 1)
                    {
                        _logger.LogInformation("History not yet available for prompt: {PromptId}, retrying in {Delay}ms...", promptId, retryDelayMs);
                        continue;
                    }
                    
                    _logger.LogWarning("No history found for prompt after {Attempts} attempts: {PromptId}", maxRetries, promptId);
                    return new List<byte[]>();
                }

                var promptHistory = history[promptId];
                var images = new List<byte[]>();

                if (promptHistory.Outputs == null)
                {
                    _logger.LogWarning("No outputs in history for prompt: {PromptId}", promptId);
                    return images;
                }

                // Find SaveImage nodes in outputs
                foreach (var output in promptHistory.Outputs.Values)
                {
                    if (output.Images != null)
                    {
                        foreach (var imageInfo in output.Images)
                        {
                            var imageData = await DownloadImageAsync(imageInfo, cancellationToken);
                            if (imageData != null)
                            {
                                images.Add(imageData);
                            }
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} images for prompt: {PromptId}", images.Count, promptId);
                return images;
            }
            catch (Exception ex) when (ex is not InvalidOperationException && attempt < maxRetries - 1)
            {
                _logger.LogDebug(ex, "Error fetching images (attempt {Attempt}/{MaxRetries}), retrying...", attempt + 1, maxRetries);
            }
        }

        throw new InvalidOperationException("Failed to retrieve images from ComfyUI after multiple attempts");
    }

    /// <summary>
    /// Download a specific image from ComfyUI
    /// </summary>
    private async Task<byte[]?> DownloadImageAsync(
        ComfyImageInfo imageInfo, 
        CancellationToken cancellationToken)
    {
        var httpEndpoint = GetHttpEndpoint();
        var url = $"{httpEndpoint}/view?filename={imageInfo.Filename}&subfolder={imageInfo.Subfolder}&type={imageInfo.Type}";

        _logger.LogDebug("Downloading image: {Filename}", imageInfo.Filename);

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download image: {Filename}", imageInfo.Filename);
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading image: {Filename}", imageInfo.Filename);
            return null;
        }
    }

    private string GetHttpEndpoint()
    {
        // Convert ws:// to http:// or wss:// to https://
        var endpoint = _settings.ApiEndpoint
            .Replace("ws://", "http://")
            .Replace("wss://", "https://");
        
        return endpoint.TrimEnd('/');
    }

    private string GetWebSocketEndpoint()
    {
        var endpoint = _settings.ApiEndpoint.TrimEnd('/');
        
        // Ensure it's a WebSocket URL
        if (!endpoint.StartsWith("ws://") && !endpoint.StartsWith("wss://"))
        {
            endpoint = endpoint.Replace("http://", "ws://").Replace("https://", "wss://");
        }

        return $"{endpoint}/ws?clientId={_clientId}";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _webSocket?.Dispose();
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}
