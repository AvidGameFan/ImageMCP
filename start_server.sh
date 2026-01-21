#!/bin/bash
# Start ImageMCP Server with custom configuration

# Default values
TEMPLATE="./templates/default_workflow.json"
ENDPOINT="ws://127.0.0.1:8188"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --template)
      TEMPLATE="$2"
      shift 2
      ;;
    --endpoint)
      ENDPOINT="$2"
      shift 2
      ;;
    --help)
      echo "Usage: $0 [OPTIONS]"
      echo ""
      echo "Options:"
      echo "  --template PATH    Path to ComfyUI workflow JSON template"
      echo "  --endpoint URL     ComfyUI WebSocket endpoint (default: ws://127.0.0.1:8188)"
      echo "  --help            Show this help message"
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      echo "Use --help for usage information"
      exit 1
      ;;
  esac
done

# Start the server
echo "Starting ImageMCP Server..."
echo "Template: $TEMPLATE"
echo "ComfyUI Endpoint: $ENDPOINT"
echo ""

dotnet run --project ImageMCP.csproj \
  --template "$TEMPLATE" \
  --comfyui-endpoint "$ENDPOINT"
