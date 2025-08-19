using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RhinoMcpServer.Models;
using RhinoMcpServer.Services;
using RhinoMcpServer.Tools;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables("RHINO_MCP_");
builder.Configuration.AddCommandLine(args);

// Configure options
builder.Services.Configure<RhinoMcpServerOptions>(
    builder.Configuration.GetSection(RhinoMcpServerOptions.ConfigSection));

// Add services
builder.Services.AddSingleton<IRhinoService, RhinoService>();

// Configure MCP server
var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
var mcpServerOptions = new McpServerOptions
{
    ServerInfo = new Implementation
    {
        Name = "rhino-mcp-server",
        Version = version
    },
    Capabilities = new ServerCapabilities
    {
        Tools = new ToolsCapability
        {
            ListChanged = true
        }
    }
};

builder.Services.AddMcpServer(mcpServerOptions);

// Register MCP tools
builder.Services.AddMcpTool<GetActiveDocumentTool>();
builder.Services.AddMcpTool<ListOpenDocumentsTool>();
builder.Services.AddMcpTool<OpenDocumentTool>();
builder.Services.AddMcpTool<GetDocumentInfoTool>();
builder.Services.AddMcpTool<GetAllObjectsTool>();
builder.Services.AddMcpTool<GetObjectsByLayerTool>();
builder.Services.AddMcpTool<GetLayersTool>();

var host = builder.Build();

try
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var options = host.Services.GetRequiredService<IOptions<RhinoMcpServerOptions>>();
    
    logger.LogInformation("Starting Rhino MCP Server v{Version}...", version);
    logger.LogInformation("Configuration: MaxObjects={MaxObjects}, HeadlessMode={HeadlessMode}, Timeout={Timeout}s",
        options.Value.MaxObjectsPerRequest,
        options.Value.EnableHeadlessMode,
        options.Value.TimeoutSeconds);
    
    await host.RunAsync();
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "A critical error occurred while running the MCP server");
    Environment.Exit(1);
}