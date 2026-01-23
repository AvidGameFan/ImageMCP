#!/bin/bash
# ImageMCP Server Startup Script
# Bash script to start the ImageMCP server with optional parameters

# Default values
TEMPLATE="./workflows/default_workflow.json"
ENDPOINT="ws://127.0.0.1:8188"
LISTEN="http://localhost:5243"

# Parse command line arguments
show_help() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --template PATH    Path to ComfyUI workflow JSON template"
    echo "  --endpoint URL     ComfyUI WebSocket endpoint (default: ws://127.0.0.1:8188)"
    echo "  --listen URL       ImageMCP HTTP listen address (default: http://localhost:5243)"
    echo "                     Use http://0.0.0.0:5243 for network access"
    echo "  --help             Show this help message"
    exit 0
}

while [[ $# -gt 0 ]]; do
    case $1 in
        --template)
            TEMPLATE="$2"
            shift 2
            ;;
        --endpoint|--comfyui-endpoint)
            ENDPOINT="$2"
            shift 2
            ;;
        --listen|--urls)
            LISTEN="$2"
            shift 2
            ;;
        --help)
            show_help
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

echo "===================================="
echo "  ImageMCP Server"
echo "===================================="
echo ""
echo "Starting ImageMCP Server..."
echo "Template: $TEMPLATE"
echo "ComfyUI Endpoint: $ENDPOINT"
echo "Listening on: $LISTEN"
echo ""

# Start the server
dotnet run --project ImageMCP.csproj -- --template "$TEMPLATE" --comfyui-endpoint "$ENDPOINT" --listen "$LISTEN"

