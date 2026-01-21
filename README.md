# ImageMCP - ComfyUI Image Generation MCP Server

An MCP (Model Context Protocol) server that enables LM Studio and other MCP-compatible clients to generate images using ComfyUI's Stable Diffusion workflows.

**Now working with LM Studio via HTTP mode!**

![ImageMCP running in LM Studio](ImageMCP%20example.png)

## 🎯 Overview

ImageMCP bridges the gap between AI language models and image generation by providing a clean MCP interface to ComfyUI. This allows language models to generate images on-demand during conversations.

## 🏗️ Architecture

```
LM Studio ↔ MCP Protocol (HTTP) ↔ ImageMCP Server ↔ WebSocket ↔ ComfyUI API
```

**Key Components:**
- **MCP Server**: Handles JSON-RPC 2.0 protocol via HTTP (EDMCP-style)
- **Workflow Template Manager**: Parses and modifies ComfyUI JSON workflows
- **ComfyUI Client**: WebSocket/HTTP communication with ComfyUI
- **Auto-format Detection**: Handles both UI and API format workflows

## ✨ Features

- ✅ **HTTP MCP Support**: JSON-RPC 2.0 over HTTP for LM Studio integration
- ✅ **ComfyUI Workflow Integration**: Parse, modify, and execute ComfyUI workflows
- ✅ **Smart Prompt Injection**: Automatically finds and replaces CLIP Text Encode nodes
- ✅ **Auto-format Detection**: Works with both UI and API format workflows
- ✅ **Configurable Templates**: Support for custom workflow JSON files
- ✅ **WebSocket Monitoring**: Real-time progress tracking via ComfyUI WebSocket
- ✅ **Base64 Image Return**: Direct image data in MCP format
- ✅ **Comprehensive Logging**: Detailed execution tracking with Microsoft.Extensions.Logging
- ✅ **Retry Logic**: Automatic retry for history retrieval
- ✅ **Error Handling**: Graceful timeout and error management

## 📋 Prerequisites

1. **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
2. **ComfyUI** - Running instance with WebSocket API enabled
   - Download from: https://github.com/comfyanonymous/ComfyUI
   - Default URL: `ws://127.0.0.1:8188`
3. **LM Studio** - For MCP client support
   - Download from: https://lmstudio.ai/
4. **Stable Diffusion Models** - At least one checkpoint model in ComfyUI

## 🚀 Getting Started

### Step 1: Install ComfyUI

```bash
# Clone ComfyUI
git clone https://github.com/comfyanonymous/ComfyUI.git
cd ComfyUI

# Install dependencies (Python 3.10+)
pip install -r requirements.txt

# Desktop version (optional)
Installing the ComfyUI desktop app is optional but recommended for ease of use.

# Download a model (e.g., Stable Diffusion XL)
# Place models in: ComfyUI/models/checkpoints/
# Example: sd_xl_base_1.0_0.9vae.safetensors
```

### Step 2: Start ComfyUI

```bash
# From ComfyUI directory
python main.py

# ComfyUI will start on: http://127.0.0.1:8188
# WebSocket available at: ws://127.0.0.1:8188/ws
```
(Or, just run ComfyUI desktop app if installed)

### Step 3: Build ImageMCP

```bash
# Clone this repository
git clone <your-repo-url>
cd ImageMCP

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests (optional)
dotnet test
```
(or download and install build package)

### Step 4: Configure ImageMCP

Edit `appsettings.json`:

```json
{
  "ComfyUI": {
    "ApiEndpoint": "ws://127.0.0.1:8188",
    "DefaultTemplate": "workflows/default_workflow.json",
    "TimeoutSeconds": 300,
    "PollIntervalSeconds": 1
  },
  "MCP": {
    "ServerName": "ImageMCP",
    "ServerVersion": "1.0.0-dev",
    "HttpPort": 5243,
    "HttpUrl": "http://localhost:5243"
  }
}
```

### Step 5: Create a Workflow Template

**Important**: Use **API format** workflows for best results!

1. Design a workflow in ComfyUI's web interface
2. Click **"Save (API Format)"** or export via the API
3. Save to `workflows/default_workflow.json`

**The workflow must contain**:
- At least one `CLIPTextEncode` node for the positive prompt
- Optionally a second `CLIPTextEncode` node for negative prompt  
- A `SaveImage` node for output

**Note**: ImageMCP auto-detects workflow format and handles both UI and API formats, but API format is recommended for reliability.

### Step 6: Run ImageMCP

```bash
dotnet run --project ImageMCP
```

Or from the project directory:

```bash
dotnet run
```

Output:
```
info: Startup[0]
      ComfyUI settings loaded: ApiEndpoint=ws://127.0.0.1:8188, DefaultTemplate=workflows/default_workflow.json
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5243
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Step 7: Configure LM Studio

**ImageMCP uses HTTP mode (EDMCP-style)** for LM Studio integration:

1. Open LM Studio
2. Go to **Developer** tab → **MCP Servers**
3. Click **"Configure"** or edit `mcp.json`
4. Add the ImageMCP server configuration:

```json
{
  "mcpServers": {
    "imagemcp": {
      "url": "http://localhost:5243/mcp"
    }
  }
}
```

5. Click **"Save"** or save the file
6. Restart LM Studio or reload the MCP configuration

The server should connect and show as available in LM Studio.

### Step 8: Test Image Generation

In LM Studio chat, ask the model:

> "Generate an image of a serene mountain landscape at sunset with vibrant colors"

The model will:
1. Recognize the image generation request
2. Call the `generate_image` tool with your prompt
3. ImageMCP will inject the prompt into the workflow
4. ComfyUI will generate the image (this may take 1-2 minutes)
5. The image will be returned and displayed in LM Studio

**Example output**:
```
info: ImageGen[0]
      Template is already in API format, injecting prompts directly
info: ImageGen[0]
      Submitting workflow with prompt: serene mountain landscape at sunset with vibrant colors
info: ImageMCP.Services.ComfyUIClient[0]
      Workflow submitted successfully. Prompt ID: 628d6005-8bee-4236-b15b-eefdd39efeb6
info: ImageMCP.Services.ComfyUIClient[0]
      Workflow execution completed: 628d6005-8bee-4236-b15b-eefdd39efeb6
info: ImageMCP.Services.ComfyUIClient[0]
      Retrieved 1 images for prompt: 628d6005-8bee-4236-b15b-eefdd39efeb6
```

## 🔧 Configuration Options

### ComfyUI Settings

| Setting | Description | Default |
|---------|-------------|---------|
| `ApiEndpoint` | ComfyUI WebSocket URL | `ws://127.0.0.1:8188` |
| `DefaultTemplate` | Path to default workflow JSON | `workflows/default_workflow.json` |
| `TimeoutSeconds` | Max execution time | `300` (5 minutes) |
| `PollIntervalSeconds` | WebSocket poll interval | `1` |

### MCP Settings

| Setting | Description | Default |
|---------|-------------|---------|
| `ServerName` | Display name for MCP | `ImageMCP` |
| `ServerVersion` | Server version | `1.0.0-dev` |
| `HttpPort` | HTTP server port | `5243` |
| `HttpUrl` | HTTP server URL | `http://localhost:5243` |

## 📝 Tool Parameters

### `generate_image`

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `prompt` | string | ✅ Yes | Text description of the image to generate |
| `negative_prompt` | string | ❌ No | What to avoid in the image (default: "text, watermark, low quality, blurry, distorted") |
| `template` | string | ❌ No | Custom workflow template path (default: from config) |

## 📁 Project Structure

```
ImageMCP/
├── Models/
│   ├── ComfyUISettings.cs          # Configuration model
│   ├── ComfyWorkflow.cs            # Workflow JSON structure
│   ├── ComfyPromptRequest.cs       # API request/response models
│   ├── McpMessage.cs               # MCP protocol messages
│   └── McpSettings.cs              # MCP configuration
├── Services/
│   ├── ComfyUIClient.cs            # WebSocket/HTTP client for ComfyUI
│   └── WorkflowTemplateManager.cs  # Template loading and prompt injection
├── workflows/
│   └── default_workflow.json       # Default SDXL workflow (API format)
├── ImageMCP.Tests/
│   ├── Services/                   # Service layer tests
│   └── Integration/                # End-to-end tests
├── Program.cs                      # Application entry point (HTTP server)
├── appsettings.json                # Configuration file
└── README.md                       # This file
```

## 🧪 Testing

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Category

```bash
# Unit tests only
dotnet test --filter Category=Unit

# Integration tests (requires ComfyUI running)
dotnet test --filter Category=Integration
```

### Test Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 🔍 Troubleshooting

### ComfyUI Connection Failed

**Error**: `Could not connect to ComfyUI at ws://127.0.0.1:8188`

**Solutions**:
1. Verify ComfyUI is running: Open `http://127.0.0.1:8188` in browser
2. Check firewall settings
3. Verify WebSocket endpoint in `appsettings.json`
4. Ensure ComfyUI is accessible (not bound to different interface)

### No History Found After Generation

**Error**: `No history found for prompt after 10 attempts`

**Solutions**:
1. ComfyUI may be under heavy load - the retry logic waits up to 11 seconds
2. Check ComfyUI console for errors during execution
3. Verify the workflow completed successfully in ComfyUI's UI
4. Check ComfyUI's `output` directory for generated images

### Configuration Not Loading

**Error**: `Workflow template not found: path/to/template.json`

**Solutions**:
1. Ensure `appsettings.json` is being copied to output directory
2. Check that the project file includes `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`
3. Verify the path in `ComfyUI:DefaultTemplate` is relative to the executable
4. Check that the `workflows` folder exists in the output directory

### Workflow Format Issues

**Error**: `Workflow does not contain nodes` or `Required input is missing`

**Solutions**:
1. Use **API format** workflows (Save → API Format in ComfyUI)
2. Ensure the workflow has all required nodes (CLIPTextEncode, SaveImage)
3. Test the workflow in ComfyUI first before using as template
4. Check the ImageMCP logs to see which format was detected

### No Images Generated

**Error**: `Image generation completed but no images were produced`

**Solutions**:
1. Check ComfyUI console for errors
2. Verify workflow has a `SaveImage` node
3. Check that a model is loaded in ComfyUI
4. Ensure sufficient disk space for outputs
5. Verify the SaveImage node is connected to the generation pipeline

### Workflow Execution Timeout

**Error**: `Workflow execution timed out after 300 seconds`

**Solutions**:
1. Increase `TimeoutSeconds` in `appsettings.json`
2. Use a faster sampler (euler, deis, dpm++)
3. Reduce `num_inference_steps` (try 20-30 for SDXL)
4. Check GPU/CPU utilization
5. Ensure ComfyUI isn't queuing multiple requests

### LM Studio Connection Issues

**Error**: LM Studio shows "Server not responding" or timeout

**Solutions**:
1. Verify ImageMCP is running (`http://localhost:5243/health` should return `{"status":"ok"}`)
2. Check the port isn't already in use
3. Ensure `appsettings.json` has correct `HttpUrl` and `HttpPort`
4. Check LM Studio's Developer Console for error messages
5. Restart both ImageMCP and LM Studio

## 📖 How It Works

### 1. HTTP MCP Server

ImageMCP runs a simple HTTP server (EDMCP-style) that exposes two endpoints:

- **GET `/mcp`**: Returns server info (name, version, protocols)
- **POST `/mcp`**: Handles JSON-RPC 2.0 requests (initialize, tools/list, tools/call)

### 2. Workflow Template Loading & Auto-Detection

```csharp
var workflowJson = await File.ReadAllTextAsync(templatePath);
var template = JsonDocument.Parse(workflowJson);

// Auto-detect format
bool isApiFormat = !template.RootElement.TryGetProperty("nodes", out _);
```

ImageMCP automatically detects:
- **API format**: Node IDs as top-level keys (e.g., `{"3": {...}, "6": {...}}`)
- **UI format**: Has `nodes` and `links` arrays (exported from ComfyUI UI)

### 3. Prompt Injection (API Format)

```csharp
// For API format workflows
var workflow = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(workflowJson);

foreach (var (nodeId, node) in workflow)
{
    if (node.class_type == "CLIPTextEncode")
    {
        node.inputs.text = positivePrompt; // or negativePrompt
    }
}
```

The manager uses heuristics to identify positive vs. negative prompts:
- **Negative**: Contains keywords like "worst quality", "bad", "watermark", "text,"
- **Positive**: Everything else (first `CLIPTextEncode` node found)

### 4. Workflow Submission

```csharp
var requestJson = $$"""
{
    "prompt": {{workflowJson}},
    "client_id": "{{_clientId}}"
}
""";

await httpClient.PostAsync($"{comfyUIEndpoint}/prompt", content);
// Returns prompt_id for tracking
```

### 5. Progress Monitoring via WebSocket

```csharp
await webSocket.ConnectAsync($"ws://127.0.0.1:8188/ws?clientId={clientId}");

while (true)
{
    var message = await ReceiveWebSocketMessage();
    
    if (message.type == "executed" && message.data.prompt_id == promptId)
    {
        return true; // Execution complete
    }
}
```

Listens for WebSocket messages:
- `progress`: Step updates during generation
- `executed`: Completion signal
- `execution_error`: Failures
- `execution_cached`: Cached results

### 6. Image Retrieval with Retry Logic

```csharp
// Wait 1 second for history to finalize
await Task.Delay(1000);

// Retry up to 10 times
for (int attempt = 0; attempt < 10; attempt++)
{
    var history = await httpClient.GetAsync($"/history/{promptId}");
    
    if (history.ContainsKey(promptId))
    {
        var images = ExtractImages(history[promptId].outputs);
        return images; // Success!
    }
    
    await Task.Delay(1000); // Wait and retry
}
```

This accounts for the slight delay between ComfyUI signaling completion and writing history.

## 🔐 Security Considerations

- **Local Only**: Default configuration binds to localhost
- **No Authentication**: ComfyUI API is unauthenticated by default
- **File Access**: Templates can access local filesystem
- **Resource Limits**: Set appropriate timeouts to prevent abuse

## 🛣️ Roadmap

- [ ] Support for multiple concurrent generations
- [ ] Queue management and prioritization
- [ ] Progress reporting to MCP client
- [ ] ControlNet and LoRA support
- [ ] Batch generation
- [ ] Image-to-image workflows
- [ ] Upscaling workflows
- [ ] Model selection via parameters
- [ ] Workflow caching
- [ ] Remote ComfyUI support with authentication

## 📄 License

[Your License Here]

## 🤝 Contributing

Contributions welcome! Please submit pull requests or open issues.

## 📞 Support

- Issues: [GitHub Issues](your-repo-url/issues)
- Discussions: [GitHub Discussions](your-repo-url/discussions)

## 🙏 Acknowledgments

- [ComfyUI](https://github.com/comfyanonymous/ComfyUI) - The powerful Stable Diffusion workflow engine
- [LM Studio](https://lmstudio.ai/) - MCP client implementation
- [Model Context Protocol](https://modelcontextprotocol.io/) - The protocol specification

---

**Built with ❤️ for the AI community**
    "ServerName": "ComfyUI Image Generator",
    "ServerVersion": "1.0.0"
  }
}
```

### Command Line Arguments

```bash
dotnet run --template ./workflows/sdxl_basic.json --comfyui-endpoint ws://127.0.0.1:8188
```

## Usage

### Starting the Server

```bash
cd ImageMCP
dotnet run
```

### With Custom Configuration

```bash
dotnet run --template ./custom_workflow.json --comfyui-endpoint ws://192.168.1.100:8188
```

### Integration with LM Studio

Configure LM Studio to use this MCP server by adding to your MCP configuration:

```json
{
  "mcpServers": {
    "comfyui": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/path/to/ImageMCP"],
      "env": {}
    }
  }
}
```

## Development Status

### ? Phase 1: Foundation (Current)
- [x] MCP protocol implementation
- [x] Stdio transport layer
- [x] Tool registry system
- [x] Configuration management
- [x] Basic unit tests

### ?? Phase 2: ComfyUI Integration (In Progress)
- [ ] WebSocket client for ComfyUI API
- [ ] JSON workflow template parser
- [ ] CLIP node detection and modification
- [ ] Image generation orchestration

### ?? Phase 3: Advanced Features (Planned)
- [ ] Multi-template support
- [ ] Advanced workflow customization
- [ ] Image result caching
- [ ] Progress tracking

## Testing

Run all tests:

```bash
cd ImageMCP.Tests
dotnet test
```

Run with coverage:

```bash
dotnet test /p:CollectCoverage=true
```

## Requirements

- .NET 10.0 SDK
- ComfyUI running locally or on network
- LM Studio with MCP support

## Tool Definition

The server exposes the following tool to LM Studio:

**generate_image**
- **Description**: Generate an image using ComfyUI based on a text prompt
- **Parameters**:
  - `prompt` (required): Text description of the image
  - `negative_prompt` (optional): What to avoid in the image
  - `template` (optional): Custom workflow template path

## License

[Add your license here]

## Contributing

[Add contribution guidelines here]
