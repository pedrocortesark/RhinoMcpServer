using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RhinoMcpServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace RhinoMcpServer.Tools;

public class OpenDocumentTool : McpServerTool
{
    private readonly IRhinoService _rhinoService;
    private readonly ILogger<OpenDocumentTool> _logger;

    public OpenDocumentTool(IRhinoService rhinoService, ILogger<OpenDocumentTool> logger)
    {
        _rhinoService = rhinoService;
        _logger = logger;
    }

    public override Tool ProtocolTool => new()
    {
        Name = "open_document",
        Description = "Opens a Rhino 3DM file (in headless mode if possible) and returns document information including geometry count, layers, etc.",
        InputSchema = new
        {
            type = "object",
            properties = new
            {
                filePath = new
                {
                    type = "string",
                    description = "Full path to the Rhino 3DM file to open"
                }
            },
            required = new[] { "filePath" }
        }
    };

    public override async ValueTask<CallToolResult> InvokeAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Params.Arguments == null)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new[]
                    {
                        TextContent.CreateFrom("Missing required argument: filePath")
                    }
                };
            }

            var args = JsonSerializer.Deserialize<JsonElement>(request.Params.Arguments);
            
            if (!args.TryGetProperty("filePath", out var filePathElement))
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new[]
                    {
                        TextContent.CreateFrom("Missing required argument: filePath")
                    }
                };
            }

            var filePath = filePathElement.GetString();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new[]
                    {
                        TextContent.CreateFrom("File path cannot be empty")
                    }
                };
            }

            _logger.LogInformation("Opening document: {FilePath}", filePath);

            var documentInfo = await _rhinoService.OpenDocumentAsync(filePath);
            
            if (documentInfo == null)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new[]
                    {
                        TextContent.CreateFrom($"Failed to open document: {filePath}. Please check the file path and ensure it's a valid Rhino 3DM file.")
                    }
                };
            }

            var json = JsonSerializer.Serialize(documentInfo, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return new CallToolResult
            {
                Content = new[]
                {
                    TextContent.CreateFrom($"Successfully opened Rhino document:\n\n```json\n{json}\n```")
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening document");
            return new CallToolResult
            {
                IsError = true,
                Content = new[]
                {
                    TextContent.CreateFrom($"Error opening document: {ex.Message}")
                }
            };
        }
    }
}