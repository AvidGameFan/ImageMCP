# ImageMCP Quick Start Guide

Get up and running with ImageMCP in 5 minutes! Generate AI images directly from LM Studio conversations.

![ImageMCP in action](ImageMCP%20example.png)

## Prerequisites Checklist

- [ ] **ComfyUI** installed and running
- [ ] At least one **Stable Diffusion model** in ComfyUI
- [ ] **LM Studio** installed (for MCP client)
- [ ] **ImageMCP package** downloaded (Windows .zip or Linux .tar.gz)

**No .NET installation required!** - The packages are self-contained.

## Step-by-Step Setup

### 1. Download ImageMCP

Choose the package for your operating system:

- **Windows**: `ImageMCP_server.zip`
- **Linux**: `ImageMCP_server_linux.tar.gz`

### 2. Extract the Package

**Windows:**
1. Right-click `ImageMCP_server.zip`
2. Select "Extract All..."
3. Choose a destination folder (e.g., `C:\ImageMCP`)

**Linux:**
```bash
tar -xzf ImageMCP_server_linux.tar.gz
cd imagemcp
chmod +x start-imagemcp.sh
```

### 3. Start ComfyUI

**Before running ImageMCP, make sure ComfyUI is running!**

```bash
# Navigate to your ComfyUI directory
cd C:\path\to\ComfyUI
python main.py
```
Or, just click the ComfyUI icon if you've installed the desktop version.

Wait for: `To see the GUI go to: http://127.0.0.1:8188`

Verify it's running by opening http://127.0.0.1:8188 in your browser.

### 4. Start ImageMCP Server

**Windows:**
- Double-click `start-imagemcp.bat`
- Or from PowerShell:
  ```powershell
  .\ImageMCP.exe
  ```

**Linux:**
```bash
./start-imagemcp.sh
```

Expected output:
```
======================================
   ImageMCP MCP Server
======================================
Listening on: http://localhost:5243
ComfyUI: ws://127.0.0.1:8188

info: Startup[0]
      ComfyUI settings loaded: ApiEndpoint=ws://127.0.0.1:8188, DefaultTemplate=workflows/default_workflow.json
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5243
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**Test the server:**
```powershell
# Windows PowerShell
Invoke-WebRequest http://localhost:5243/health

# Linux
curl http://localhost:5243/health
```

Should respond: `{"status":"ok"}`

### 5. Configure LM Studio

**ImageMCP uses HTTP mode for LM Studio integration:**

1. Open **LM Studio**
2. Go to **Developer** tab → **MCP Servers**
3. Click **"Configure"** or edit the MCP configuration
4. Add this server configuration:

```json
{
  "mcpServers": {
    "imagemcp": {
      "url": "http://localhost:5243/mcp"
    }
  }
}
```

5. **Save** the configuration
6. **Restart LM Studio** or reload the MCP configuration

The ImageMCP server should appear as connected in LM Studio's Developer panel.

Note that you also need to load an LLM model that can call MCP tools, such as Gemma 3.

### 6. Test Image Generation

In LM Studio chat, try one of these prompts:

> "Generate an image of a serene mountain landscape at sunset with vibrant colors"

> "Create a photorealistic image of a cat sitting on a windowsill"

> "Generate a cyberpunk city street scene at night with neon lights"

The model will:
1. Recognize the image generation request
2. Call the `generate_image` tool with your prompt
3. ImageMCP will inject your prompt into the workflow
4. ComfyUI will generate the image (may take 1-2 minutes)
5. The image will be displayed in LM Studio

**Example console output during generation:**
```
info: ImageGen[0]
      Template is already in API format, injecting prompts directly
info: ImageGen[0]
      Submitting workflow with prompt: serene mountain landscape at sunset...
info: ImageMCP.Services.ComfyUIClient[0]
      Workflow submitted successfully. Prompt ID: 628d6005-...
info: ImageMCP.Services.ComfyUIClient[0]
      Workflow execution completed: 628d6005-...
info: ImageMCP.Services.ComfyUIClient[0]
      Retrieved 1 images for prompt: 628d6005-...
```

## Network Access Configuration

### Access ImageMCP from Other Machines

By default, ImageMCP only listens on `localhost`. To allow network access:

1. **Edit `appsettings.json`:**

```json
{
  "MCP": {
    "HttpPort": 5243,
    "HttpUrl": "http://0.0.0.0:5243"
  }
}
```

2. **Restart ImageMCP**

3. **Configure LM Studio on remote machine:**

```json
{
  "mcpServers": {
    "imagemcp": {
      "url": "http://192.168.1.100:5243/mcp"
    }
  }
}
```

Replace `192.168.1.100` with your ImageMCP server's IP address.

4. **Configure Firewall:**

**Windows:**
```powershell
netsh advfirewall firewall add rule name="ImageMCP" dir=in action=allow protocol=TCP localport=5243
```

**Linux:**
```bash
sudo ufw allow 5243/tcp
```

## Troubleshooting

### "Server won't start" or "Port already in use"

**Windows:**
```powershell
# Find what's using port 5243
netstat -ano | findstr :5243
# Kill the process ... only if you know it's safe to do so (can lose data)
taskkill /PID <PID> /F
```

**Linux:**
```bash
# Find what's using port 5243
sudo lsof -i :5243
# Kill the process ... only if you know it's safe to do so (can lose data)
sudo kill -9 <PID>
```

### "Cannot connect to ComfyUI"

**Fixes:**
1. Verify ComfyUI is running: Open http://127.0.0.1:8188 in browser
2. Check `appsettings.json` has correct endpoint: `"ApiEndpoint": "ws://127.0.0.1:8188"`
3. Test WebSocket: Use browser console `new WebSocket("ws://127.0.0.1:8188/ws")`
4. Check firewall isn't blocking port 8188

### "LM Studio shows timeout" or "Server not responding"

**Fixes:**
1. Verify ImageMCP is running: http://localhost:5243/health should return `{"status":"ok"}`
2. Check LM Studio configuration has correct URL: `http://localhost:5243/mcp`
3. Restart both ImageMCP and LM Studio
4. Check LM Studio's Developer Console for detailed errors

### "Workflow template not found"

**Fixes:**
1. Ensure `workflows/default_workflow.json` exists in the extracted package
2. Verify the path in `appsettings.json`:
   ```json
   {
     "ComfyUI": {
       "DefaultTemplate": "workflows/default_workflow.json"
     }
   }
   ```
3. Use absolute path if relative path fails: `"C:\\ImageMCP\\workflows\\default_workflow.json"`

### "No history found for prompt" after generation

**Explanation:** ComfyUI sometimes needs a moment to finalize image history after execution completes.

**ImageMCP handles this automatically** with retry logic (up to 11 seconds). If images still don't appear:

1. Check ComfyUI console for errors during generation
2. Verify the workflow completed in ComfyUI's web interface
3. Check ComfyUI's `output` directory for generated images
4. Ensure the workflow has a `SaveImage` node connected properly

### "Image generation failed: Required input is missing"

**This means the workflow format is incorrect.**

**Fixes:**
1. **Use API format workflows** - In ComfyUI, click "Save (API Format)" not just "Save"
2. Test the workflow in ComfyUI's UI first before using as template
3. Ensure all required nodes are present:
   - `CLIPTextEncode` (for prompts)
   - `SaveImage` (for output)
   - Model loader node
   - KSampler or similar generation node
4. Check all nodes are properly connected

### "Workflow execution timed out after 300 seconds"

**Fixes:**
1. Increase timeout in `appsettings.json`:
   ```json
   {
     "ComfyUI": {
       "TimeoutSeconds": 600
     }
   }
   ```
2. Use a faster sampler in your workflow (euler, dpm++, deis)
3. Reduce `num_inference_steps` to 20-30
4. Check GPU isn't overloaded with other tasks
5. Ensure ComfyUI isn't queuing multiple requests

## Custom Workflow Templates

### Using Your Own Workflow

1. **Create workflow in ComfyUI's web interface**
2. **Export in API format**:
   - Click "Save (API Format)" button
   - Or use ComfyUI's API export feature
3. **Save to the workflows directory**:
   ```
   workflows/my_custom_workflow.json
   ```
4. **Update `appsettings.json`**:
   ```json
   {
     "ComfyUI": {
       "DefaultTemplate": "workflows/my_custom_workflow.json"
     }
   }
   ```
5. **Restart ImageMCP**

### Template Requirements

**Required nodes:**
- ✅ At least one `CLIPTextEncode` node (for positive prompt)
- ✅ A `SaveImage` node (for output)
- ✅ Valid connections between all nodes

**Optional but recommended:**
- ➕ Second `CLIPTextEncode` for negative prompt
- ➕ `EmptyLatentImage` for size control
- ➕ `KSampler` for generation control

### How Prompt Injection Works

ImageMCP automatically:

1. **Detects workflow format** (API or UI format)
2. **Finds `CLIPTextEncode` nodes** in the workflow
3. **Identifies positive vs. negative** based on content:
   - **Negative**: Contains "worst quality", "bad", "watermark", "text,"
   - **Positive**: First non-negative CLIPTextEncode node
4. **Replaces the text** with your prompt
5. **Submits to ComfyUI**

## Advanced Configuration

### Change Server Port

Edit `appsettings.json`:

```json
{
  "MCP": {
    "HttpPort": 8080,
    "HttpUrl": "http://localhost:8080"
  }
}
```

Then update LM Studio configuration to use `http://localhost:8080/mcp`.

### Connect to Remote ComfyUI

Edit `appsettings.json`:

```json
{
  "ComfyUI": {
    "ApiEndpoint": "ws://192.168.1.100:8188"
  }
}
```

### Enable Debug Logging

Edit `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "ImageMCP": "Debug"
    }
  }
}
```

Restart ImageMCP to see detailed logs of prompt injection, workflow submission, etc.

## Next Steps

- ✅ Try different prompts and see how the AI generates images
- ✅ Create custom workflows in ComfyUI for specific styles
- ✅ Adjust generation parameters (steps, sampler, etc.)
- ✅ Share your workflow templates with others
- ✅ Join the community and contribute!

## Getting Help

- **Check the logs** - ImageMCP shows detailed console output
- **Test in ComfyUI first** - Verify workflows work standalone
- **Read the README** - Full documentation with troubleshooting
- **Report issues** - GitHub issues with logs and configuration

---

**You're all set!** Start generating amazing AI images directly from LM Studio conversations. 🎨✨

## Performance Tips

### Fast Generation (5-10 seconds)
- Use Turbo/Lightning models
- Reduce steps to 4-8
- Use `euler` or `deis` sampler
- Lower resolution (512x512 or 768x768)

### High Quality (30-60 seconds)
- Use full SDXL models
- 20-30 steps
- Use `dpm++` or `euler_ancestral` sampler
- Higher resolution (1024x1024+)

### Balanced (15-20 seconds)
- SDXL optimized models (e.g., animagineXL)
- 15-20 steps
- `euler` sampler
- 832x1216 or 1024x1024

## Next Steps

- ? Read the full [README.md](README.md) for detailed documentation
- ? Explore custom workflow templates
- ? Configure advanced settings in `appsettings.json`
- ? Check out the unit tests for usage examples
- ? Join discussions and report issues

## Common Use Cases

### Portrait Generation
```
Prompt: "professional portrait of a person, studio lighting, detailed face, high quality"
Negative: "distorted features, bad anatomy, low quality"
```

### Landscape Generation
```
Prompt: "beautiful landscape with mountains, lake, sunset, vibrant colors, detailed"
Negative: "text, watermark, low quality, blurry"
```

### Artistic Styles
```
Prompt: "oil painting of a castle, impressionist style, vibrant brushstrokes, masterpiece"
Negative: "photo, realistic, modern, low quality"
```

### Anime Style
```
Prompt: "anime girl, detailed eyes, colorful hair, masterpiece, best quality"
Negative: "worst quality, low quality, bad anatomy, 3d, realistic"
Model: animagineXL or similar anime-focused model
```

---

**Happy generating! ??**

For help: [Open an issue](https://github.com/AvidGameFan/ImageMCP/issues) or check the [README](README.md)
