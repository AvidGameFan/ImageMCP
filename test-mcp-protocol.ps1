# Test ImageMCP Server - MCP Protocol Test
# This script simulates what LM Studio does when communicating with the MCP server

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  ImageMCP Server MCP Protocol Test" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script tests the MCP server by sending JSON-RPC messages via stdio." -ForegroundColor Yellow
Write-Host "This simulates how LM Studio communicates with the server." -ForegroundColor Yellow
Write-Host ""

# Test messages
$initializeMessage = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"TestClient","version":"1.0.0"}}}'
$toolsListMessage = '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}'
$pingMessage = '{"jsonrpc":"2.0","id":3,"method":"ping","params":{}}'

Write-Host "Starting ImageMCP server..." -ForegroundColor Green

# Create a process to run dotnet run
$process = New-Object System.Diagnostics.Process
$process.StartInfo.FileName = "dotnet"
$process.StartInfo.Arguments = "run --project ImageMCP.csproj"
$process.StartInfo.UseShellExecute = $false
$process.StartInfo.RedirectStandardInput = $true
$process.StartInfo.RedirectStandardOutput = $true
$process.StartInfo.RedirectStandardError = $true
$process.StartInfo.CreateNoWindow = $true

# Event handlers for output
$outputBuilder = New-Object System.Text.StringBuilder
$errorBuilder = New-Object System.Text.StringBuilder

$outputHandler = {
    if (-not [string]::IsNullOrEmpty($EventArgs.Data)) {
        $outputBuilder.AppendLine($EventArgs.Data)
        Write-Host "STDOUT: $($EventArgs.Data)" -ForegroundColor Gray
    }
}

$errorHandler = {
    if (-not [string]::IsNullOrEmpty($EventArgs.Data)) {
        $errorBuilder.AppendLine($EventArgs.Data)
        Write-Host "STDERR: $($EventArgs.Data)" -ForegroundColor Red
    }
}

$process.add_OutputDataReceived($outputHandler)
$process.add_ErrorDataReceived($errorHandler)

try {
    # Start the process
    $null = $process.Start()
    $process.BeginOutputReadLine()
    $process.BeginErrorReadLine()
    
    Write-Host "Server started. Waiting for initialization..." -ForegroundColor Green
    Start-Sleep -Seconds 2
    
    # Test 1: Initialize
    Write-Host ""
    Write-Host "Test 1: Sending 'initialize' request..." -ForegroundColor Cyan
    $process.StandardInput.WriteLine($initializeMessage)
    $process.StandardInput.Flush()
    Start-Sleep -Seconds 1
    
    # Test 2: Tools List
    Write-Host ""
    Write-Host "Test 2: Sending 'tools/list' request..." -ForegroundColor Cyan
    $process.StandardInput.WriteLine($toolsListMessage)
    $process.StandardInput.Flush()
    Start-Sleep -Seconds 1
    
    # Test 3: Ping
    Write-Host ""
    Write-Host "Test 3: Sending 'ping' request..." -ForegroundColor Cyan
    $process.StandardInput.WriteLine($pingMessage)
    $process.StandardInput.Flush()
    Start-Sleep -Seconds 1
    
    Write-Host ""
    Write-Host "All tests sent. Waiting for responses..." -ForegroundColor Green
    Start-Sleep -Seconds 2
    
    Write-Host ""
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host "  Test Complete!" -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "If you see JSON responses above containing:" -ForegroundColor Yellow
    Write-Host "  - protocolVersion for 'initialize'" -ForegroundColor White
    Write-Host "  - tools array with 'generate_image' for 'tools/list'" -ForegroundColor White
    Write-Host "  - empty result for 'ping'" -ForegroundColor White
    Write-Host "Then the MCP server is working correctly!" -ForegroundColor Green
    
} finally {
    # Cleanup
    Write-Host ""
    Write-Host "Stopping server..." -ForegroundColor Yellow
    if (-not $process.HasExited) {
        $process.Kill()
    }
    $process.Dispose()
}

Write-Host ""
Write-Host "Note: When LM Studio runs ImageMCP, it handles all of this automatically." -ForegroundColor Cyan
Write-Host "You should NOT run ImageMCP manually - let LM Studio manage it!" -ForegroundColor Yellow
