using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RhinoMcpServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace RhinoMcpServer.Tools;

public class GetAllObjectsTool : McpServerTool
{
    private readonly IRhinoService _rhinoService;
    private readonly ILogger<GetAllObjectsTool> _logger;

    public GetAllObjectsTool(IRhinoService rhinoService, ILogger<GetAllObjectsTool> logger)
    {
        _rhinoService = rhinoService;
        _logger = logger;
    }

    public override Tool ProtocolTool => new()
    {
        Name = "get_all_objects",
        Description = "Retrieves all geometry objects from the active Rhino document with their properties, attributes, and metadata.",
        InputSchema = JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                includeHidden = new
                {
                    type = "boolean",
                    description = "Include hidden objects in the result (default: false)",
                    @default = false
                },
                maxObjects = new
                {
                    type = "integer",
                    description = "Maximum number of objects to return (default: 1000, max: 10000)",
                    @default = 1000,
                    minimum = 1,
                    maximum = 10000
                }
            },
            required = new string[] { }
        })
    };

    public override async ValueTask<CallToolResult> InvokeAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        try
        {
            var includeHidden = false;
            var maxObjects = 1000;
            
            if (request.Params.Arguments != null)
            {
                if (request.Params.Arguments.TryGetValue("includeHidden", out var hiddenElement))
                {
                    includeHidden = hiddenElement.GetBoolean();
                }
                
                if (request.Params.Arguments.TryGetValue("maxObjects", out var maxElement))
                {
                    maxObjects = Math.Min(Math.Max(maxElement.GetInt32(), 1), 10000);
                }
            }

            // Removed logging to keep stdout clean for MCP protocol

            var allObjects = await _rhinoService.GetAllObjectsAsync();
            
            if (allObjects.Count == 0)
            {
                return new CallToolResult
                {
                    Content = new List<ContentBlock> {
                        new TextContentBlock { Text = "No objects found in the active Rhino document." }
                    }
                };
            }

            var filteredObjects = allObjects.AsEnumerable();
            
            if (!includeHidden)
            {
                filteredObjects = filteredObjects.Where(obj => obj.IsVisible);
            }

            var objects = filteredObjects.Take(maxObjects).ToList();

            var summary = new
            {
                totalObjects = allObjects.Count,
                filteredObjects = objects.Count,
                filters = new
                {
                    includeHidden,
                    maxObjects
                },
                objectsByType = objects.GroupBy(o => o.GeometryType).ToDictionary(g => g.Key, g => g.Count()),
                objectsByLayer = objects.GroupBy(o => o.LayerName).ToDictionary(g => g.Key, g => g.Count()),
                objects = objects
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
                    new TextContentBlock { Text = $"Rhino Objects Retrieved ({objects.Count} of {allObjects.Count} total):\n\n```json\n{json}\n```" }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all objects");
            return new CallToolResult
            {
                IsError = true,
                Content = new List<ContentBlock> {
                    new TextContentBlock { Text = $"Error retrieving objects: {ex.Message}" }
                }
            };
        }
    }
}