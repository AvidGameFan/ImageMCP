@echo off
REM Start ImageMCP Server with custom configuration

REM Default values
set TEMPLATE=./templates/default_workflow.json
set ENDPOINT=ws://127.0.0.1:8188
set LISTEN=http://localhost:5243

REM Parse command line arguments
:parse_args
if "%~1"=="" goto run_server
if /i "%~1"=="--template" (
    set TEMPLATE=%~2
    shift
    shift
    goto parse_args
)
if /i "%~1"=="--endpoint" (
    set ENDPOINT=%~2
    shift
    shift
    goto parse_args
)
if /i "%~1"=="--listen" (
    set LISTEN=%~2
    shift
    shift
    goto parse_args
)
if /i "%~1"=="--help" (
    echo Usage: %~nx0 [OPTIONS]
    echo.
    echo Options:
    echo   --template PATH    Path to ComfyUI workflow JSON template
    echo   --endpoint URL     ComfyUI WebSocket endpoint (default: ws://127.0.0.1:8188^)
    echo   --listen URL       ImageMCP HTTP listen address (default: http://localhost:5243^)
    echo                      Use http://0.0.0.0:5243 for network access
    echo   --help            Show this help message
    exit /b 0
)
echo Unknown option: %~1
echo Use --help for usage information
exit /b 1

:run_server
echo Starting ImageMCP Server...
echo Template: %TEMPLATE%
echo ComfyUI Endpoint: %ENDPOINT%
echo Listening on: %LISTEN%
echo.

dotnet run --project ImageMCP.csproj --template "%TEMPLATE%" --comfyui-endpoint "%ENDPOINT%" --listen "%LISTEN%"
