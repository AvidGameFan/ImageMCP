# Start ImageMCP in HTTP mode (EDMCP-style)
# This mode runs as a persistent HTTP server

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ImageMCP Server - HTTP Mode" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Starting ImageMCP in HTTP mode (EDMCP-style)..." -ForegroundColor Green
Write-Host "Server will listen on http://localhost:5243/mcp" -ForegroundColor Yellow
Write-Host ""
Write-Host "LM Studio Configuration:" -ForegroundColor Cyan
Write-Host '  "url": "http://localhost:5243/mcp"' -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
Write-Host ""

# Run in HTTP mode
dotnet run --project ImageMCP.csproj -- --mode=http
