using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RhinoMcpServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace RhinoMcpServer.Tools;

public class GetDocumentInfoTool : McpServerTool
{
    private readonly IRhinoService _rhinoService;
    private readonly ILogger<GetDocumentInfoTool> _logger;

    public GetDocumentInfoTool(IRhinoService rhinoService, ILogger<GetDocumentInfoTool> logger)
    {
        _rhinoService = rhinoService;
        _logger = logger;
    }

    public override Tool ProtocolTool => new()
    {
        Name = "get_document_info",
        Description = "Retrieves comprehensive information about the active Rhino document including metadata, statistics, and properties.",
        InputSchema = new
        {
            type = "object",
            properties = new
            {
                includeStatistics = new
                {
                    type = "boolean",
                    description = "Include detailed statistics about objects and layers (default: true)",
                    @default = true
                }
            },
            required = new string[] { }
        }
    };

    public override async ValueTask<CallToolResult> InvokeAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        try
        {
            var includeStatistics = true;
            
            if (request.Params.Arguments != null)
            {
                var args = JsonSerializer.Deserialize<JsonElement>(request.Params.Arguments);
                if (args.TryGetProperty("includeStatistics", out var statsElement))
                {
                    includeStatistics = statsElement.GetBoolean();
                }
            }

            _logger.LogInformation("Getting document information (includeStatistics: {IncludeStats})", includeStatistics);

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

            object responseData = documentInfo;

            if (includeStatistics)
            {
                var layers = await _rhinoService.GetLayersAsync();
                var objects = await _rhinoService.GetAllObjectsAsync();
                
                var statistics = new
                {
                    documentInfo,
                    statistics = new
                    {
                        totalLayers = layers.Count,
                        visibleLayers = layers.Count(l => l.IsVisible),
                        lockedLayers = layers.Count(l => l.IsLocked),
                        totalObjects = objects.Count,
                        visibleObjects = objects.Count(o => o.IsVisible),
                        selectedObjects = objects.Count(o => o.IsSelected),
                        objectsByType = objects.GroupBy(o => o.GeometryType).ToDictionary(g => g.Key, g => g.Count()),
                        objectsByLayer = objects.GroupBy(o => o.LayerName).ToDictionary(g => g.Key, g => g.Count())
                    }
                };

                responseData = statistics;
            }

            var json = JsonSerializer.Serialize(responseData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return new CallToolResult
            {
                Content = new[]
                {
                    TextContent.CreateFrom($"Document Information{(includeStatistics ? " with Statistics" : "")}:\n\n```json\n{json}\n```")
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document information");
            return new CallToolResult
            {
                IsError = true,
                Content = new[]
                {
                    TextContent.CreateFrom($"Error retrieving document information: {ex.Message}")
                }
            };
        }
    }
}