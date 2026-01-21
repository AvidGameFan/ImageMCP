using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImageMCP.Services;
using ImageMCP.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageMCP
{
    public class GenerateImageInput
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("negative_prompt")]
        public string? NegativePrompt { get; set; }

        [JsonPropertyName("template")]
        public string? Template { get; set; }
    }

    // JSON-RPC Request/Response types
    public class JsonRpcRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public object? Id { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("params")]
        public JsonElement? Params { get; set; }
    }

    public class JsonRpcResponse
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public object? Id { get; set; }

        [JsonPropertyName("result")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Result { get; set; }

        [JsonPropertyName("error")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonRpcError? Error { get; set; }
    }

    public class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Data { get; set; }
    }

    public class InitializeResult
    {
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = "2024-11-05";

        [JsonPropertyName("capabilities")]
        public object Capabilities { get; set; } = new { tools = new { } };

        [JsonPropertyName("serverInfo")]
        public ServerInfo ServerInfo { get; set; } = new();
    }

    public class ServerInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "ImageMCP";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0-dev";
    }

    public class Tool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("inputSchema")]
        public object InputSchema { get; set; } = new { };
    }

    public class ToolListResult
    {
        [JsonPropertyName("tools")]
        public List<Tool> Tools { get; set; } = new();
    }

    public class ToolCallResult
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; }
    }

    public class ImageToolResult
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "image";

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;

        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = "image/png";
    }

    public class ToolResult
    {
        [JsonPropertyName("content")]
        public List<object> Content { get; set; } = new();

        [JsonPropertyName("isError")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsError { get; set; } = false;
    }

    public static class Program
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static ComfyUISettings _comfySettings = new();
        private static ILoggerFactory _loggerFactory = null!;

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Create logger factory first
            _loggerFactory = LoggerFactory.Create(b => b.AddConsole());

            // Load configuration and bind to settings
            var config = builder.Configuration;
            _comfySettings = config.GetSection("ComfyUI").Get<ComfyUISettings>() ?? new ComfyUISettings();
            
            // Validate critical settings
            if (string.IsNullOrEmpty(_comfySettings.DefaultTemplate))
            {
                _loggerFactory.CreateLogger("Startup").LogError("DefaultTemplate is not configured in appsettings.json");
                throw new InvalidOperationException("DefaultTemplate must be configured in appsettings.json under ComfyUI:DefaultTemplate");
            }
            
            _loggerFactory.CreateLogger("Startup").LogInformation("ComfyUI settings loaded: ApiEndpoint={Endpoint}, DefaultTemplate={Template}", 
                _comfySettings.ApiEndpoint, _comfySettings.DefaultTemplate);

            // Set the listening URL
            builder.WebHost.UseUrls("http://localhost:5243");

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            app.UseCors("AllowAll");

            // HTTP POST endpoint for MCP (used by LM Studio)
            app.MapPost("/mcp", HandleHttpMcp);

            // HTTP GET endpoint for health/info
            app.MapGet("/mcp", async context =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    name = "ImageMCP",
                    version = "1.0.0-dev",
                    protocols = new[] { "json-rpc" }
                }));
            });

            // Health check endpoint
            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

            await app.RunAsync();
        }

        private static async Task HandleHttpMcp(HttpContext context)
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            try
            {
                context.Request.EnableBuffering();
                var bodyText = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (string.IsNullOrWhiteSpace(bodyText))
                {
                    context.Response.StatusCode = 400;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new JsonRpcResponse
                    {
                        Error = new JsonRpcError { Code = -32600, Message = "Invalid Request: empty body" }
                    }, options));
                    return;
                }

                JsonRpcRequest? request;
                try
                {
                    request = JsonSerializer.Deserialize<JsonRpcRequest>(bodyText, options);
                }
                catch (JsonException jsonEx)
                {
                    context.Response.StatusCode = 400;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new JsonRpcResponse
                    {
                        Error = new JsonRpcError
                        {
                            Code = -32700,
                            Message = "Parse error - Invalid JSON",
                            Data = jsonEx.Message
                        }
                    }, options));
                    return;
                }

                if (request == null)
                {
                    context.Response.StatusCode = 400;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new JsonRpcResponse
                    {
                        Error = new JsonRpcError { Code = -32600, Message = "Invalid Request" }
                    }, options));
                    return;
                }

                var response = await HandleMcpRequest(request);
                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(context.Response.Body, response, options);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new JsonRpcResponse
                {
                    Error = new JsonRpcError { Code = -32603, Message = "Internal error", Data = ex.Message }
                }));
            }
        }

        private static async Task<JsonRpcResponse> HandleMcpRequest(JsonRpcRequest request)
        {
            try
            {
                return request.Method switch
                {
                    "initialize" => new JsonRpcResponse
                    {
                        Id = request.Id,
                        Result = new InitializeResult()
                    },

                    "tools/list" => new JsonRpcResponse
                    {
                        Id = request.Id,
                        Result = new ToolListResult
                        {
                            Tools = new List<Tool>
                            {
                                new Tool
                                {
                                    Name = "generate_image",
                                    Description = "Generate an image using ComfyUI based on a text prompt",
                                    InputSchema = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            prompt = new
                                            {
                                                type = "string",
                                                description = "The text prompt describing the image to generate"
                                            },
                                            negative_prompt = new
                                            {
                                                type = "string",
                                                description = "What to avoid in the generated image (optional)",
                                                @default = "text, watermark, low quality, blurry, distorted"
                                            },
                                            template = new
                                            {
                                                type = "string",
                                                description = "Optional path to a custom ComfyUI workflow JSON template"
                                            }
                                        },
                                        required = new[] { "prompt" }
                                    }
                                }
                            }
                        }
                    },

                    "tools/call" => await HandleToolCall(request),

                    _ => new JsonRpcResponse
                    {
                        Id = request.Id,
                        Error = new JsonRpcError
                        {
                            Code = -32601,
                            Message = $"Method not found: {request.Method}"
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError
                    {
                        Code = -32603,
                        Message = "Internal error",
                        Data = ex.Message
                    }
                };
            }
        }

        private static async Task<JsonRpcResponse> HandleToolCall(JsonRpcRequest request)
        {
            if (request.Params == null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32602, Message = "Missing params" }
                };
            }

            var paramsObj = request.Params.Value;
            var toolName = paramsObj.GetProperty("name").GetString();
            var toolInput = paramsObj.GetProperty("arguments");

            if (toolName != "generate_image")
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32602, Message = $"Unknown tool: {toolName}" }
                };
            }

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var input = JsonSerializer.Deserialize<GenerateImageInput>(toolInput.GetRawText(), options);
            if (input == null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32602, Message = "Invalid tool input" }
                };
            }

            try
            {
                var negativePrompt = input.NegativePrompt ?? "text, watermark, low quality, blurry, distorted";
                var templatePath = input.Template ?? _comfySettings.DefaultTemplate;
                
                if (string.IsNullOrWhiteSpace(templatePath))
                {
                    throw new InvalidOperationException("No workflow template specified. Either provide a template path or configure DefaultTemplate in appsettings.json");
                }
                
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Workflow template not found: {templatePath}");
                }

                var comfyClient = new ComfyUIClient(_comfySettings, _loggerFactory.CreateLogger<ComfyUIClient>());
                var templateManager = new WorkflowTemplateManager(_loggerFactory.CreateLogger<WorkflowTemplateManager>());

                // Load template
                var workflowJson = await File.ReadAllTextAsync(templatePath);
                var template = JsonDocument.Parse(workflowJson);
                
                // Check if it's UI format (has "nodes" array) or API format (node IDs as keys)
                bool isApiFormat = !template.RootElement.TryGetProperty("nodes", out _);
                
                string finalWorkflowJson;
                if (isApiFormat)
                {
                    _loggerFactory.CreateLogger("ImageGen").LogInformation("Template is already in API format, injecting prompts directly");
                    // Template is already in API format - inject prompts directly
                    finalWorkflowJson = await templateManager.InjectPromptsApiFormat(workflowJson, input.Prompt, negativePrompt);
                }
                else
                {
                    _loggerFactory.CreateLogger("ImageGen").LogInformation("Template is in UI format, converting to API format");
                    // Template is in UI format - convert it
                    var modifiedWorkflow = templateManager.InjectPrompts(template, input.Prompt, negativePrompt);
                    var apiWorkflow = templateManager.ConvertToApiFormat(modifiedWorkflow);
                    finalWorkflowJson = JsonSerializer.Serialize(apiWorkflow);
                }
                
                _loggerFactory.CreateLogger("ImageGen").LogInformation("Submitting workflow with prompt: {Prompt}", input.Prompt);

                await comfyClient.ConnectAsync();
                var promptId = await comfyClient.SubmitWorkflowAsync(finalWorkflowJson);

                var completed = await comfyClient.WaitForCompletionAsync(promptId);
                if (!completed)
                {
                    throw new InvalidOperationException("Workflow execution did not complete successfully");
                }

                var images = await comfyClient.GetImagesAsync(promptId);

                if (images.Count == 0)
                {
                    var toolResult = new ToolResult
                    {
                        Content = new List<object>
                        {
                            new ToolCallResult
                            {
                                Type = "text",
                                Text = "Image generation completed but no images were produced."
                            }
                        },
                        IsError = false
                    };

                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Result = toolResult
                    };
                }

                var base64Image = Convert.ToBase64String(images[0]);

                var result = new ToolResult
                {
                    Content = new List<object>
                    {
                        new ImageToolResult
                        {
                            Type = "image",
                            Data = base64Image,
                            MimeType = "image/png"
                        }
                    },
                    IsError = false
                };

                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                var errorResult = new ToolResult
                {
                    Content = new List<object>
                    {
                        new ToolCallResult
                        {
                            Type = "text",
                            Text = $"Image generation failed: {ex.Message}"
                        }
                    },
                    IsError = true
                };

                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = errorResult
                };
            }
        }
    }
}
