using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RhinoMcpServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace RhinoMcpServer.Tools;

public class GetActiveDocumentTool : McpServerTool
{
    private readonly IRhinoService _rhinoService;
    private readonly ILogger<GetActiveDocumentTool> _logger;

    public GetActiveDocumentTool(IRhinoService rhinoService, ILogger<GetActiveDocumentTool> logger)
    {
        _rhinoService = rhinoService;
        _logger = logger;
    }

    public override Tool ProtocolTool => new()
    {
        Name = "get_active_document",
        Description = "Retrieves detailed information about the currently active Rhino document, including file path, units, layer count, and object count.",
        InputSchema = new
        {
            type = "object",
            properties = new { },
            required = new string[] { }
        }
    };

    public override async ValueTask<CallToolResult> InvokeAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting active document information");

            var documentInfo = await _rhinoService.GetActiveDocumentInfoAsync();
            
            if (documentInfo == null)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new[]
                    {
                        TextContent.CreateFrom("No active Rhino document found. Please ensure Rhino is running and a document is open.")
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
                    TextContent.CreateFrom($"Active Rhino Document Information:\n\n```json\n{json}\n```")
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active document");
            return new CallToolResult
            {
                IsError = true,
                Content = new[]
                {
                    TextContent.CreateFrom($"Error retrieving active document information: {ex.Message}")
                }
            };
        }
    }
}