using ImageMCP.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ImageMCP.Services;

/// <summary>
/// Manages ComfyUI workflow templates and prompt injection
/// </summary>
public class WorkflowTemplateManager
{
    private readonly ILogger<WorkflowTemplateManager> _logger;

    public WorkflowTemplateManager(ILogger<WorkflowTemplateManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Load a workflow template from a JSON file
    /// </summary>
    public async Task<JsonDocument> LoadTemplateAsync(string templatePath)
    {
        if (!File.Exists(templatePath))
        {
            templatePath = Program._comfySettings.DefaultTemplate; //"workflows\\defaultworkflow.json";
            //throw new FileNotFoundException($"Workflow template not found: {templatePath}");
        }

        _logger.LogDebug("Loading workflow template from: {TemplatePath}", templatePath);
        
        var json = await File.ReadAllTextAsync(templatePath);
        return JsonDocument.Parse(json);
    }

    /// <summary>
    /// Inject prompts into a workflow template
    /// </summary>
    public JsonDocument InjectPrompts(JsonDocument template, string positivePrompt, string? negativePrompt = null)
    {
        _logger.LogDebug("Injecting prompts into workflow template");

        // Parse the template as a mutable object
        var workflowJson = template.RootElement.GetRawText();
        using var doc = JsonDocument.Parse(workflowJson);
        
        // We need to work with the raw JSON and rebuild it
        var options = new JsonSerializerOptions { WriteIndented = false };
        var workflow = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(workflowJson, options);

        if (workflow == null)
        {
            throw new InvalidOperationException("Failed to parse workflow template");
        }

        // Find and modify CLIP Text Encode nodes
        if (workflow.TryGetValue("nodes", out var nodesElement))
        {
            var nodes = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(nodesElement.GetRawText(), options);
            
            if (nodes == null)
            {
                throw new InvalidOperationException("Failed to parse workflow nodes");
            }

            int positivePromptCount = 0;
            int negativePromptCount = 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                
                if (node.TryGetValue("type", out var typeElement) && 
                    typeElement.GetString() == "CLIPTextEncode")
                {
                    // Get the widgets_values array
                    if (node.TryGetValue("widgets_values", out var widgetsElement))
                    {
                        var widgets = JsonSerializer.Deserialize<List<string>>(widgetsElement.GetRawText(), options);
                        
                        if (widgets != null && widgets.Count > 0)
                        {
                            var currentText = widgets[0].ToLowerInvariant();
                            
                            // Determine if this is a positive or negative prompt based on content
                            bool isNegativePrompt = currentText.Contains("negative") ||
                                                   currentText.Contains("worst quality") ||
                                                   currentText.Contains("low quality") ||
                                                   currentText.Contains("bad") ||
                                                   currentText.Contains("watermark") ||
                                                   currentText.Contains("text,");

                            if (isNegativePrompt && negativePrompt != null && negativePromptCount == 0)
                            {
                                _logger.LogDebug("Found negative prompt node at index {Index}, replacing with: {Prompt}", i, negativePrompt);
                                widgets[0] = negativePrompt;
                                negativePromptCount++;
                            }
                            else if (!isNegativePrompt && positivePromptCount == 0)
                            {
                                _logger.LogDebug("Found positive prompt node at index {Index}, replacing with: {Prompt}", i, positivePrompt);
                                widgets[0] = positivePrompt;
                                positivePromptCount++;
                            }

                            // Update the node with new widgets_values
                            node["widgets_values"] = JsonSerializer.SerializeToElement(widgets, options);
                            nodes[i] = node;
                        }
                    }
                }
            }

            if (positivePromptCount == 0)
            {
                _logger.LogWarning("No CLIPTextEncode node found for positive prompt");
            }

            // Update the workflow with modified nodes
            workflow["nodes"] = JsonSerializer.SerializeToElement(nodes, options);
        }

        // Serialize back to JsonDocument
        var modifiedJson = JsonSerializer.Serialize(workflow, options);
        return JsonDocument.Parse(modifiedJson);
    }

    /// <summary>
    /// Inject prompts into an API-format workflow (node IDs as keys)
    /// </summary>
    public async Task<string> InjectPromptsApiFormat(string workflowJson, string positivePrompt, string? negativePrompt = null)
    {
        _logger.LogDebug("Injecting prompts into API-format workflow");

        var options = new JsonSerializerOptions { WriteIndented = false };
        var workflow = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(workflowJson, options);

        if (workflow == null)
        {
            throw new InvalidOperationException("Failed to parse API workflow");
        }

        int positivePromptCount = 0;
        int negativePromptCount = 0;

        // Iterate through all nodes (keys are node IDs)
        foreach (var kvp in workflow.ToList())
        {
            var nodeId = kvp.Key;
            var nodeElement = kvp.Value;

            // Check if this node has class_type = CLIPTextEncode
            if (nodeElement.TryGetProperty("class_type", out var classTypeElement) &&
                classTypeElement.GetString() == "CLIPTextEncode")
            {
                // Get the inputs
                if (nodeElement.TryGetProperty("inputs", out var inputsElement))
                {
                    var inputs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(inputsElement.GetRawText(), options);
                    
                    if (inputs != null && inputs.TryGetValue("text", out var textElement))
                    {
                        var currentText = textElement.GetString()?.ToLowerInvariant() ?? "";
                        
                        // Determine if this is negative or positive
                        bool isNegativePrompt = currentText.Contains("negative") ||
                                               currentText.Contains("worst quality") ||
                                               currentText.Contains("low quality") ||
                                               currentText.Contains("bad") ||
                                               currentText.Contains("watermark") ||
                                               currentText.Contains("text,");

                        if (isNegativePrompt && negativePrompt != null && negativePromptCount == 0)
                        {
                            _logger.LogDebug("Found negative prompt node {NodeId}, replacing with: {Prompt}", nodeId, negativePrompt);
                            inputs["text"] = JsonSerializer.SerializeToElement(negativePrompt, options);
                            negativePromptCount++;
                        }
                        else if (!isNegativePrompt && positivePromptCount == 0)
                        {
                            _logger.LogDebug("Found positive prompt node {NodeId}, replacing with: {Prompt}", nodeId, positivePrompt);
                            inputs["text"] = JsonSerializer.SerializeToElement(positivePrompt, options);
                            positivePromptCount++;
                        }

                        // Rebuild the node with updated inputs
                        var nodeDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(nodeElement.GetRawText(), options);
                        if (nodeDict != null)
                        {
                            nodeDict["inputs"] = JsonSerializer.SerializeToElement(inputs, options);
                            workflow[nodeId] = JsonSerializer.SerializeToElement(nodeDict, options);
                        }
                    }
                }
            }
        }

        if (positivePromptCount == 0)
        {
            _logger.LogWarning("No CLIPTextEncode node found for positive prompt in API workflow");
        }

        return JsonSerializer.Serialize(workflow, options);
    }

    /// <summary>
    /// Convert workflow to ComfyUI API format
    /// </summary>
    public Dictionary<string, ComfyNodeData> ConvertToApiFormat(JsonDocument workflow)
    {
        _logger.LogDebug("Converting workflow to ComfyUI API format");

        var apiWorkflow = new Dictionary<string, ComfyNodeData>();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        if (!workflow.RootElement.TryGetProperty("nodes", out var nodesElement))
        {
            throw new InvalidOperationException("Workflow does not contain nodes");
        }

        var nodes = JsonSerializer.Deserialize<List<WorkflowNode>>(nodesElement.GetRawText(), options);
        
        if (nodes == null)
        {
            throw new InvalidOperationException("Failed to parse workflow nodes");
        }

        // Build a map of link ID -> (source_node, source_slot, target_input_name)
        var linkMap = new Dictionary<int, (string sourceNode, int sourceSlot, string targetInputName)>();
        
        if (workflow.RootElement.TryGetProperty("links", out var linksElement) && 
            linksElement.ValueKind == JsonValueKind.Array)
        {
            var links = JsonSerializer.Deserialize<List<List<JsonElement>>>(linksElement.GetRawText(), options);
            
            if (links != null)
            {
                foreach (var link in links)
                {
                    if (link.Count >= 6)
                    {
                        // Link format: [link_id, source_node, source_slot, target_node, target_slot, target_input_name]
                        var linkId = link[0].GetInt32();
                        var sourceNode = link[1].GetInt32().ToString();
                        var sourceSlot = link[2].GetInt32();
                        var targetInputName = link[5].GetString() ?? "";
                        
                        linkMap[linkId] = (sourceNode, sourceSlot, targetInputName);
                    }
                }
            }
        }

        foreach (var node in nodes)
        {
            var nodeData = new ComfyNodeData
            {
                ClassType = node.Type,
                Inputs = new Dictionary<string, object>()
            };

            // Add widget values first
            if (node.WidgetsValues != null && node.WidgetsValues.Value.ValueKind != JsonValueKind.Null)
            {
                var widgetValues = JsonSerializer.Deserialize<List<JsonElement>>(
                    node.WidgetsValues.Value.GetRawText(), options);

                if (widgetValues != null && node.Inputs != null)
                {
                    // Map widget values to inputs
                    int widgetIndex = 0;
                    foreach (var input in node.Inputs)
                    {
                        if (input.Widget != null && widgetIndex < widgetValues.Count)
                        {
                            var value = widgetValues[widgetIndex];
                            nodeData.Inputs[input.Name] = ParseJsonElement(value);
                            widgetIndex++;
                        }
                    }
                }
            }

            // Add linked inputs
            if (node.Inputs != null)
            {
                foreach (var input in node.Inputs)
                {
                    if (input.Link != null && input.Link.Value.ValueKind == JsonValueKind.Number)
                    {
                        var linkId = input.Link.Value.GetInt32();
                        if (linkMap.TryGetValue(linkId, out var linkInfo))
                        {
                            // Use the link's target input name, or fall back to the input name
                            var inputName = string.IsNullOrEmpty(linkInfo.targetInputName) ? input.Name : linkInfo.targetInputName;
                            nodeData.Inputs[inputName] = new object[] { linkInfo.sourceNode, linkInfo.sourceSlot };
                        }
                    }
                }
            }

            apiWorkflow[node.Id.ToString()] = nodeData;
        }

        _logger.LogDebug("Converted {NodeCount} nodes to API format", apiWorkflow.Count);
        return apiWorkflow;
    }

    private static object ParseJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray().Select(ParseJsonElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ParseJsonElement(p.Value)),
            _ => element.GetRawText()
        };
    }
}
