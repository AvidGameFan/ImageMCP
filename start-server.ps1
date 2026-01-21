# ImageMCP Server Startup Script
# PowerShell script to start the ImageMCP server with optional parameters

param(
    [string]$ComfyUIEndpoint = "ws://127.0.0.1:8188",
    [string]$Template = "workflows/default_workflow.json",
    [int]$Timeout = 300
)

Write-Host "====================================" -ForegroundColor Cyan
Write-Host "  ImageMCP Server Startup Script" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Check if ComfyUI is running
Write-Host "Checking ComfyUI availability..." -ForegroundColor Yellow
$httpEndpoint = $ComfyUIEndpoint -replace "ws://", "http://" -replace "wss://", "https://"
try {
    $response = Invoke-WebRequest -Uri "$httpEndpoint/system_stats" -Method GET -TimeoutSec 2 -ErrorAction SilentlyContinue
    Write-Host "? ComfyUI is running at $httpEndpoint" -ForegroundColor Green
} catch {
    Write-Host "? WARNING: Cannot connect to ComfyUI at $httpEndpoint" -ForegroundColor Red
    Write-Host "  Make sure ComfyUI is running before using image generation features." -ForegroundColor Yellow
    Write-Host ""
}

# Check if template exists
if (Test-Path $Template) {
    Write-Host "? Workflow template found: $Template" -ForegroundColor Green
} else {
    Write-Host "? WARNING: Template not found: $Template" -ForegroundColor Red
    Write-Host "  Using default template instead." -ForegroundColor Yellow
    $Template = "workflows/default_workflow.json"
}

Write-Host ""
Write-Host "Starting ImageMCP Server with:" -ForegroundColor Cyan
Write-Host "  ComfyUI Endpoint: $ComfyUIEndpoint" -ForegroundColor White
Write-Host "  Default Template: $Template" -ForegroundColor White
Write-Host "  Timeout: $Timeout seconds" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
Write-Host ""

# Start the server
dotnet run --project ImageMCP.csproj -- --comfyui-endpoint="$ComfyUIEndpoint" --template="$Template"
