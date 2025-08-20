using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RhinoMcpServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace RhinoMcpServer.Tools;

public class GetObjectsByLayerTool : McpServerTool
{
    private readonly IRhinoService _rhinoService;
    private readonly ILogger<GetObjectsByLayerTool> _logger;

    public GetObjectsByLayerTool(IRhinoService rhinoService, ILogger<GetObjectsByLayerTool> logger)
    {
        _rhinoService = rhinoService;
        _logger = logger;
    }

    public override Tool ProtocolTool => new()
    {
        Name = "get_objects_by_layer",
        Description = "Retrieves all geometry objects from a specific layer in the active Rhino document.",
        InputSchema = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                layerName = new
                {
                    type = "string",
                    description = "Name of the layer to retrieve objects from"
                },
                includeHidden = new
                {
                    type = "boolean",
                    description = "Include hidden objects in the result (default: false)",
                    @default = false
                }
            },
            required = new[] { "layerName" }
        })
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
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = "Missing required argument: layerName" }
                    }
                };
            }

            if (!request.Params.Arguments.TryGetValue("layerName", out var layerNameElement))
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = "Missing required argument: layerName" }
                    }
                };
            }

            var layerName = layerNameElement.GetString();
            if (string.IsNullOrWhiteSpace(layerName))
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = "Layer name cannot be empty" }
                    }
                };
            }

            var includeHidden = false;
            if (request.Params.Arguments.TryGetValue("includeHidden", out var hiddenElement))
            {
                includeHidden = hiddenElement.GetBoolean();
            }

            // Removed logging to keep stdout clean for MCP protocol

            var objects = await _rhinoService.GetObjectsByLayerAsync(layerName);
            
            if (objects.Count == 0)
            {
                var allLayers = await _rhinoService.GetLayersAsync();
                var layerExists = allLayers.Any(l => l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase));
                
                if (!layerExists)
                {
                    var availableLayers = allLayers.Select(l => l.Name).ToList();
                    return new CallToolResult
                    {
                        IsError = true,
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = $"Layer '{layerName}' not found. Available layers: {string.Join(", ", availableLayers)}" }
                        }
                    };
                }

                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"No objects found on layer '{layerName}'." }
                    }
                };
            }

            var filteredObjects = objects.AsEnumerable();
            if (!includeHidden)
            {
                filteredObjects = filteredObjects.Where(obj => obj.IsVisible);
            }

            var result = filteredObjects.ToList();

            var summary = new
            {
                layerName = layerName,
                totalObjectsOnLayer = objects.Count,
                visibleObjects = result.Count,
                includeHidden = includeHidden,
                objectsByType = result.GroupBy(o => o.GeometryType).ToDictionary(g => g.Key, g => g.Count()),
                objects = result
            };

            var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Objects from Layer '{layerName}' ({result.Count} objects):\n\n```json\n{json}\n```" }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting objects by layer");
            return new CallToolResult
            {
                IsError = true,
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Error retrieving objects from layer: {ex.Message}" }
                }
            };
        }
    }
}