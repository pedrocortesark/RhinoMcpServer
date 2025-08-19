using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RhinoMcpServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace RhinoMcpServer.Tools;

public class ListOpenDocumentsTool : McpServerTool
{
    private readonly IRhinoService _rhinoService;
    private readonly ILogger<ListOpenDocumentsTool> _logger;

    public ListOpenDocumentsTool(IRhinoService rhinoService, ILogger<ListOpenDocumentsTool> logger)
    {
        _rhinoService = rhinoService;
        _logger = logger;
    }

    public override Tool ProtocolTool => new()
    {
        Name = "list_open_documents",
        Description = "Retrieves a list of all currently open Rhino documents with their basic information.",
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
            _logger.LogInformation("Listing open documents");

            var documents = await _rhinoService.GetOpenDocumentsAsync();
            
            if (documents.Count == 0)
            {
                return new CallToolResult
                {
                    Content = new[]
                    {
                        TextContent.CreateFrom("No open Rhino documents found. Please ensure Rhino is running and has documents open.")
                    }
                };
            }

            var json = JsonSerializer.Serialize(documents, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return new CallToolResult
            {
                Content = new[]
                {
                    TextContent.CreateFrom($"Open Rhino Documents ({documents.Count} found):\n\n```json\n{json}\n```")
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing open documents");
            return new CallToolResult
            {
                IsError = true,
                Content = new[]
                {
                    TextContent.CreateFrom($"Error retrieving open documents: {ex.Message}")
                }
            };
        }
    }
}