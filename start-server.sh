#!/bin/bash
# ImageMCP Server Startup Script
# Bash script to start the ImageMCP server with optional parameters

COMFYUI_ENDPOINT="${1:-ws://127.0.0.1:8188}"
TEMPLATE="${2:-workflows/default_workflow.json}"
TIMEOUT="${3:-300}"

echo "===================================="
echo "  ImageMCP Server Startup Script"
echo "===================================="
echo ""

# Check if ComfyUI is running
echo "Checking ComfyUI availability..."
HTTP_ENDPOINT=$(echo "$COMFYUI_ENDPOINT" | sed 's/ws:\/\//http:\/\//g' | sed 's/wss:\/\//https:\/\//g')
if curl -s --max-time 2 "$HTTP_ENDPOINT/system_stats" > /dev/null 2>&1; then
    echo "? ComfyUI is running at $HTTP_ENDPOINT"
else
    echo "? WARNING: Cannot connect to ComfyUI at $HTTP_ENDPOINT"
    echo "  Make sure ComfyUI is running before using image generation features."
    echo ""
fi

# Check if template exists
if [ -f "$TEMPLATE" ]; then
    echo "? Workflow template found: $TEMPLATE"
else
    echo "? WARNING: Template not found: $TEMPLATE"
    echo "  Using default template instead."
    TEMPLATE="workflows/default_workflow.json"
fi

echo ""
echo "Starting ImageMCP Server with:"
echo "  ComfyUI Endpoint: $COMFYUI_ENDPOINT"
echo "  Default Template: $TEMPLATE"
echo "  Timeout: $TIMEOUT seconds"
echo ""
echo "Press Ctrl+C to stop the server"
echo ""

# Start the server
dotnet run --project ImageMCP.csproj -- --comfyui-endpoint="$COMFYUI_ENDPOINT" --template="$TEMPLATE"
