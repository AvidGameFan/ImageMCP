@echo off
REM Start ImageMCP Server with custom configuration

REM Default values
set TEMPLATE=./templates/default_workflow.json
set ENDPOINT=ws://127.0.0.1:8188

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
if /i "%~1"=="--help" (
    echo Usage: %~nx0 [OPTIONS]
    echo.
    echo Options:
    echo   --template PATH    Path to ComfyUI workflow JSON template
    echo   --endpoint URL     ComfyUI WebSocket endpoint (default: ws://127.0.0.1:8188^)
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
echo.

dotnet run --project ImageMCP.csproj --template "%TEMPLATE%" --comfyui-endpoint "%ENDPOINT%"
