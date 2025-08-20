using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RhinoMcpServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace RhinoMcpServer.Tools;

public class GetLayersTool : McpServerTool
{
    private readonly IRhinoService _rhinoService;
    private readonly ILogger<GetLayersTool> _logger;

    public GetLayersTool(IRhinoService rhinoService, ILogger<GetLayersTool> logger)
    {
        _rhinoService = rhinoService;
        _logger = logger;
    }

    public override Tool ProtocolTool => new()
    {
        Name = "get_layers",
        Description = "Retrieves all layers from the active Rhino document with their properties, hierarchy, and object counts.",
        InputSchema = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                includeEmpty = new
                {
                    type = "boolean",
                    description = "Include layers that have no objects (default: true)",
                    @default = true
                },
                includeHidden = new
                {
                    type = "boolean",
                    description = "Include hidden layers (default: true)",
                    @default = true
                }
            },
            required = new string[] { }
        })
    };

    public override async ValueTask<CallToolResult> InvokeAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        try
        {
            var includeEmpty = true;
            var includeHidden = true;
            
            if (request.Params.Arguments != null)
            {
                if (request.Params.Arguments.TryGetValue("includeEmpty", out var emptyElement))
                {
                    includeEmpty = emptyElement.GetBoolean();
                }
                
                if (request.Params.Arguments.TryGetValue("includeHidden", out var hiddenElement))
                {
                    includeHidden = hiddenElement.GetBoolean();
                }
            }

            _logger.LogInformation("Getting layers (includeEmpty: {IncludeEmpty}, includeHidden: {IncludeHidden})", 
                includeEmpty, includeHidden);

            var allLayers = await _rhinoService.GetLayersAsync();
            
            if (allLayers.Count == 0)
            {
                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = "No layers found in the active Rhino document." }
                    }
                };
            }

            var filteredLayers = allLayers.AsEnumerable();
            
            if (!includeEmpty)
            {
                filteredLayers = filteredLayers.Where(layer => layer.ObjectCount > 0);
            }
            
            if (!includeHidden)
            {
                filteredLayers = filteredLayers.Where(layer => layer.IsVisible);
            }

            var layers = filteredLayers.ToList();

            var topLevelLayers = layers.Where(l => l.ParentId == null).ToList();
            var childLayers = layers.Where(l => l.ParentId != null).ToList();

            var summary = new
            {
                totalLayers = allLayers.Count,
                filteredLayers = layers.Count,
                filters = new
                {
                    includeEmpty,
                    includeHidden
                },
                statistics = new
                {
                    topLevelLayers = topLevelLayers.Count,
                    childLayers = childLayers.Count,
                    visibleLayers = layers.Count(l => l.IsVisible),
                    lockedLayers = layers.Count(l => l.IsLocked),
                    layersWithObjects = layers.Count(l => l.ObjectCount > 0),
                    totalObjectsInLayers = layers.Sum(l => l.ObjectCount)
                },
                layerHierarchy = BuildLayerHierarchy(layers),
                layers = layers.OrderBy(l => l.FullPath)
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
                    new TextContentBlock { Text = $"Rhino Layers ({layers.Count} of {allLayers.Count} total):\n\n```json\n{json}\n```" }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting layers");
            return new CallToolResult
            {
                IsError = true,
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Error retrieving layers: {ex.Message}" }
                }
            };
        }
    }

    private object BuildLayerHierarchy(List<Models.RhinoLayer> layers)
    {
        var layerDict = layers.ToDictionary(l => l.Id, l => l);
        var topLevelLayers = layers.Where(l => l.ParentId == null).ToList();

        return topLevelLayers.Select(layer => BuildLayerNode(layer, layerDict)).ToList();
    }

    private object BuildLayerNode(Models.RhinoLayer layer, Dictionary<Guid, Models.RhinoLayer> layerDict)
    {
        var children = layer.ChildrenIds
            .Where(childId => layerDict.ContainsKey(childId))
            .Select(childId => BuildLayerNode(layerDict[childId], layerDict))
            .ToList();

        return new
        {
            id = layer.Id,
            name = layer.Name,
            fullPath = layer.FullPath,
            objectCount = layer.ObjectCount,
            isVisible = layer.IsVisible,
            isLocked = layer.IsLocked,
            children = children.Any() ? children : null
        };
    }
}