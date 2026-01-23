# Description: This script builds and packages the ImageMCP project for release.
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
Write-Host "  ImageMCP Release Build Script" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean -c Release
Remove-Item -Recurse -Force -Path ".\bin" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force -Path ".\obj" -ErrorAction SilentlyContinue
Write-Host "✓ Cleaned`n" -ForegroundColor Green

# Publish the executable
Write-Host "Publishing self-contained executable..." -ForegroundColor Yellow
Write-Host "  Framework: .NET 10.0" -ForegroundColor Gray
Write-Host "  Configuration: Release" -ForegroundColor Gray
Write-Host "  Runtime: win-x64" -ForegroundColor Gray
dotnet publish .\ImageMCP.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true
Write-Host ""

# Verify the build succeeded
$exePath = ".\bin\Release\net10.0\win-x64\publish\ImageMCP.exe"
if (!(Test-Path -Path $exePath)) {
    Write-Error "✗ Build failed - ImageMCP.exe not found at: $exePath`n"
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
$publishPath = ".\bin\Release\net10.0\win-x64\publish"

# Create temporary package directory
$tempDir = ".\temp_package"
if (Test-Path $tempDir) {
    Remove-Item -Recurse -Force $tempDir
}
New-Item -ItemType Directory -Path $tempDir | Out-Null

# Copy executable and configuration files
Copy-Item "$publishPath\ImageMCP.exe" -Destination $tempDir
Copy-Item "$publishPath\appsettings.json" -Destination $tempDir

# Copy workflows directory if it exists
if (Test-Path ".\workflows") {
    Copy-Item -Recurse ".\workflows" -Destination $tempDir
} else {
    Write-Warning "workflows directory not found - package will not include default workflow"
}

# Create startup script
$startupScript = @"
@echo off
REM ImageMCP Server Startup Script
REM This script is designed to run from the ImageMCP release package root directory

echo.
echo ======================================
echo    ImageMCP MCP Server
echo ======================================
echo Listening on: http://localhost:5243
echo ComfyUI: ws://127.0.0.1:8188
echo.

if not exist "ImageMCP.exe" (
    echo ERROR: ImageMCP.exe not found in current directory!
    echo.
    echo This script should be run from the ImageMCP release package directory.
    echo If you extracted ImageMCP_server.zip, run this script from that directory.
    echo.
    pause
    exit /b 1
)

echo Starting ImageMCP...
echo.

ImageMCP.exe

if errorlevel 1 (
    echo.
    echo ERROR: ImageMCP exited with code %errorlevel%
    echo.
    pause
)
"@

$startupScript | Out-File -FilePath "$tempDir\start-imagemcp.bat" -Encoding ASCII

# Create README
$readmeContent = @"
# ImageMCP Server - Windows Package

## Quick Start

1. **Ensure ComfyUI is running**
   - ComfyUI must be accessible at: http://127.0.0.1:8188
   - Start ComfyUI before running ImageMCP

2. **Run ImageMCP**
   - Double-click: ``start-imagemcp.bat``
   - Or from command line: ``ImageMCP.exe``

3. **Configure LM Studio**
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
2. Save to ``workflows\my_workflow.json``
3. Edit ``appsettings.json``:

``````json
{
  "ComfyUI": {
    "DefaultTemplate": "workflows\\my_workflow.json"
  }
}
``````

## Testing

``````powershell
# Health check
Invoke-WebRequest http://localhost:5243/health

# Should respond: {"status":"ok"}
``````

## Troubleshooting

### ComfyUI Not Accessible
- Verify ComfyUI is running: http://127.0.0.1:8188
- Check firewall settings
- Ensure WebSocket port is accessible

### Port Already in Use
``````powershell
# Find what's using port 5243
netstat -ano | findstr :5243
# Kill the process ... if you know what you're doing and it makes sense to do so
taskkill /PID <PID> /F
``````

### Workflow Not Found
- Ensure ``workflows\default_workflow.json`` exists
- Check file path in ``appsettings.json``
- Use absolute path if relative path fails

## Requirements

- Windows x64 (10/11/Server 2019+)
- No .NET runtime required (self-contained)
- ComfyUI running (default: ws://127.0.0.1:8188)
- Stable Diffusion model loaded in ComfyUI

## Support

For more information, visit: https://github.com/avidgamefan/ImageMCP
"@

$readmeContent | Out-File -FilePath "$tempDir\README.md" -Encoding UTF8

# Remove old zip if it exists
if (Test-Path ".\ImageMCP_server.zip") {
    Remove-Item ".\ImageMCP_server.zip" -Force
}

# Create zip with all files
Compress-Archive -Path "$tempDir\*" -DestinationPath ".\ImageMCP_server.zip" -Force

# Clean up temp directory
Remove-Item -Recurse -Force $tempDir

$zipSize = (Get-Item ".\ImageMCP_server.zip").Length
$zipSizeMB = [math]::Round($zipSize / 1MB, 2)

Write-Host "✓ Package created: ImageMCP_server.zip" -ForegroundColor Green
Write-Host "  Size: $zipSizeMB MB`n" -ForegroundColor Gray

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Extract ImageMCP_server.zip to your deployment folder" -ForegroundColor Gray
Write-Host "  2. Ensure ComfyUI is running" -ForegroundColor Gray
Write-Host "  3. Run: start-imagemcp.bat" -ForegroundColor Gray
Write-Host "  4. Configure LM Studio with: http://localhost:5243/mcp" -ForegroundColor Gray
Write-Host "  5. Test: Invoke-WebRequest http://localhost:5243/health`n" -ForegroundColor Gray

if (!$SkipTest) {
    Write-Host "Testing executable..." -ForegroundColor Yellow
    $exeProcess = Start-Process -FilePath $exePath -PassThru -NoNewWindow
    Start-Sleep -Seconds 3
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5243/health" -ErrorAction Stop
        Write-Host "✓ Server is running and responding!" -ForegroundColor Green
        Write-Host "  Response: $($response.Content)`n" -ForegroundColor Gray
        
        # Kill the test instance
        $exeProcess | Stop-Process -Force -ErrorAction SilentlyContinue
    } catch {
        Write-Warning "✗ Could not test server - that's ok, the build succeeded anyway"
        $exeProcess | Stop-Process -Force -ErrorAction SilentlyContinue
    }
}
