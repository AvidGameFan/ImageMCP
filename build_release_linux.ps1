# Description: This script builds and packages the ImageMCP project for Linux.
# This script should be run from the ImageMCP project root folder

param(
    [switch]$SkipTest = $false
)

# Check we're in the right directory
if (!(Test-Path -Path ".\ImageMCP.csproj")) {
    Write-Error "This script must be run from the ImageMCP folder containing ImageMCP.csproj"
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  ImageMCP Release Build Script (Linux)" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean -c Release
Remove-Item -Recurse -Force -Path ".\bin" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force -Path ".\obj" -ErrorAction SilentlyContinue
Write-Host "✓ Cleaned`n" -ForegroundColor Green

# Publish the executable
Write-Host "Publishing self-contained executable for Linux..." -ForegroundColor Yellow
Write-Host "  Framework: .NET 10.0" -ForegroundColor Gray
Write-Host "  Configuration: Release" -ForegroundColor Gray
Write-Host "  Runtime: linux-x64" -ForegroundColor Gray
dotnet publish .\ImageMCP.csproj -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true
Write-Host ""

# Verify the build succeeded
$exePath = ".\bin\Release\net10.0\linux-x64\publish\ImageMCP"
if (!(Test-Path -Path $exePath)) {
    Write-Error "✗ Build failed - ImageMCP executable not found at: $exePath`n"
    exit 1
}
Write-Host "✓ Build successful`n" -ForegroundColor Green

# Get file size
$fileSize = (Get-Item $exePath).Length
$fileSizeMB = [math]::Round($fileSize / 1MB, 2)
Write-Host "Executable Details:" -ForegroundColor Cyan
Write-Host "  Path: $exePath" -ForegroundColor Gray
Write-Host "  Size: $fileSizeMB MB ($([int]$fileSize) bytes)" -ForegroundColor Gray
Write-Host ""

# Create the release package
Write-Host "Creating release package..." -ForegroundColor Yellow
$publishPath = ".\bin\Release\net10.0\linux-x64\publish"

# Create temporary directory for packaging
$tempDir = ".\temp_linux_package"
if (Test-Path $tempDir) {
    Remove-Item -Recurse -Force $tempDir
}
New-Item -ItemType Directory -Path $tempDir | Out-Null

# Copy executable and configuration
Copy-Item "$publishPath\ImageMCP" -Destination $tempDir
Copy-Item "$publishPath\appsettings.json" -Destination $tempDir

# Copy workflows directory if it exists
if (Test-Path ".\workflows") {
    Copy-Item -Recurse ".\workflows" -Destination $tempDir
} else {
    Write-Warning "workflows directory not found - package will not include default workflow"
}

# Create startup script
$startupScript = @"
#!/bin/bash
# ImageMCP Server Startup Script
# This script is designed to run from the ImageMCP release package root directory

echo ""
echo "======================================"
echo "   ImageMCP MCP Server"
echo "======================================"
echo "Listening on: http://localhost:5243"
echo "ComfyUI: ws://127.0.0.1:8188"
echo ""

if [ ! -f "ImageMCP" ]; then
    echo "ERROR: ImageMCP executable not found in current directory!"
    echo ""
    echo "This script should be run from the ImageMCP release package directory."
    echo "If you extracted ImageMCP_server_linux.tar.gz, run this script from that directory."
    echo ""
    read -p "Press Enter to exit..."
    exit 1
fi

# Make sure ImageMCP is executable
chmod +x ImageMCP

echo "Starting ImageMCP..."
echo ""

./ImageMCP

if [ `$? -ne 0 ]; then
    echo ""
    echo "ERROR: ImageMCP exited with code `$?"
    echo ""
    read -p "Press Enter to exit..."
fi
"@

$startupScript | Out-File -FilePath "$tempDir\start-imagemcp.sh" -Encoding UTF8 -NoNewline

# Create README for Linux package
$readmeContent = @"
# ImageMCP Server - Linux Package

## Quick Start

1. **Ensure ComfyUI is running**
   - ComfyUI must be accessible at: http://127.0.0.1:8188
   - Start ComfyUI before running ImageMCP

2. Extract this archive:
   ``````bash
   tar -xzf ImageMCP_server_linux.tar.gz
   cd imagemcp
   ``````

3. Make the startup script executable:
   ``````bash
   chmod +x start-imagemcp.sh
   ``````

4. Run the server:
   ``````bash
   ./start-imagemcp.sh
   ``````

5. **Configure LM Studio**
   - Open LM Studio → Developer → MCP Servers
   - Add server configuration:
   ``````json
   {
     "mcpServers": {
       "imagemcp": {
         "url": "http://localhost:5243/mcp"
       }
     }
   }
   ``````

## Configuration

### Change Port

Edit ``appsettings.json``:

``````json
{
  "MCP": {
    "HttpPort": 8080,
    "HttpUrl": "http://localhost:8080"
  }
}
``````

### Different ComfyUI Address

Edit ``appsettings.json``:

``````json
{
  "ComfyUI": {
    "ApiEndpoint": "ws://192.168.1.100:8188"
  }
}
``````

### Custom Workflow Template

1. Export workflow from ComfyUI (Save → API Format)
2. Save to ``workflows/my_workflow.json``
3. Edit ``appsettings.json``:

``````json
{
  "ComfyUI": {
    "DefaultTemplate": "workflows/my_workflow.json"
  }
}
``````

## Run as systemd Service

Create ``/etc/systemd/system/imagemcp.service``:

``````ini
[Unit]
Description=ImageMCP MCP Server
After=network.target

[Service]
Type=simple
User=your-username
WorkingDirectory=/opt/imagemcp
ExecStart=/opt/imagemcp/ImageMCP
Restart=on-failure
RestartSec=10
Environment="ASPNETCORE_URLS=http://0.0.0.0:5243"

[Install]
WantedBy=multi-user.target
``````

Enable and start:
``````bash
sudo systemctl daemon-reload
sudo systemctl enable imagemcp
sudo systemctl start imagemcp
sudo systemctl status imagemcp
``````

## Testing

``````bash
# Health check
curl http://localhost:5243/health

# Should respond: {"status":"ok"}
``````

## Troubleshooting

### Permission Denied
``````bash
chmod +x ImageMCP
chmod +x start-imagemcp.sh
``````

### Port Already in Use
``````bash
# Find what's using port 5243
sudo lsof -i :5243
# Kill the process ... if you know what you're doing and it makes sense to do so
sudo kill -9 <PID>
``````

### ComfyUI Not Accessible
- Verify ComfyUI is running: curl http://127.0.0.1:8188
- Check firewall settings: ``sudo ufw allow 8188``
- Ensure WebSocket port is accessible

### libicu Missing (Arch Linux)
``````bash
sudo pacman -S icu
``````

### Ubuntu/Debian Dependencies
``````bash
sudo apt-get update
sudo apt-get install -y libicu-dev
``````

### Workflow Not Found
- Ensure ``workflows/default_workflow.json`` exists
- Check file path in ``appsettings.json``
- Use absolute path if relative path fails

## Requirements

- Linux x64 (Arch, Ubuntu, Debian, Fedora, etc.)
- No .NET runtime required (self-contained)
- ComfyUI running (default: ws://127.0.0.1:8188)
- Stable Diffusion model loaded in ComfyUI

## Support

For more information, visit: https://github.com/YourUsername/ImageMCP
"@

$readmeContent | Out-File -FilePath "$tempDir\README_LINUX.md" -Encoding UTF8

# Use tar.exe (built into Windows 10+) to create tar.gz
Write-Host "Creating tar.gz archive..." -ForegroundColor Yellow
Push-Location $tempDir
tar -czf "..\ImageMCP_server_linux.tar.gz" *
Pop-Location

# Clean up temp directory
Remove-Item -Recurse -Force $tempDir

$tarSize = (Get-Item ".\ImageMCP_server_linux.tar.gz").Length
$tarSizeMB = [math]::Round($tarSize / 1MB, 2)

Write-Host "✓ Package created: ImageMCP_server_linux.tar.gz" -ForegroundColor Green
Write-Host "  Size: $tarSizeMB MB`n" -ForegroundColor Gray

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Transfer ImageMCP_server_linux.tar.gz to your Linux machine" -ForegroundColor Gray
Write-Host "  2. Extract: tar -xzf ImageMCP_server_linux.tar.gz" -ForegroundColor Gray
Write-Host "  3. Make executable: chmod +x start-imagemcp.sh" -ForegroundColor Gray
Write-Host "  4. Ensure ComfyUI is running" -ForegroundColor Gray
Write-Host "  5. Run: ./start-imagemcp.sh" -ForegroundColor Gray
Write-Host "  6. Configure LM Studio with: http://localhost:5243/mcp" -ForegroundColor Gray
Write-Host "  7. Test: curl http://localhost:5243/health`n" -ForegroundColor Gray

Write-Host "Package contents:" -ForegroundColor Cyan
Write-Host "  - ImageMCP (executable)" -ForegroundColor Gray
Write-Host "  - appsettings.json (configuration)" -ForegroundColor Gray
Write-Host "  - workflows/ (default workflow templates)" -ForegroundColor Gray
Write-Host "  - start-imagemcp.sh (startup script)" -ForegroundColor Gray
Write-Host "  - README_LINUX.md (documentation)`n" -ForegroundColor Gray
