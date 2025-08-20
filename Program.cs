using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RhinoMcpServer.Models;
using RhinoMcpServer.Services;
using RhinoMcpServer.Tools;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

// CRITICAL: Disable ALL logging to prevent stdout pollution
// MCP protocol requires stdout to contain ONLY JSON messages
builder.Logging.ClearProviders();
// Only enable logging in debug builds
#if DEBUG
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
#endif
builder.Logging.SetMinimumLevel(LogLevel.Error);

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
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new Implementation
    {
        Name = "rhino-mcp-server",
        Version = version
    };
    options.Capabilities = new ServerCapabilities
    {
        Tools = new ToolsCapability
        {
            ListChanged = true
        }
    };
});

// Register MCP tools
builder.Services.AddScoped<GetActiveDocumentTool>();
builder.Services.AddScoped<ListOpenDocumentsTool>();
builder.Services.AddScoped<OpenDocumentTool>();
builder.Services.AddScoped<GetDocumentInfoTool>();
builder.Services.AddScoped<GetAllObjectsTool>();
builder.Services.AddScoped<GetObjectsByLayerTool>();
builder.Services.AddScoped<GetLayersTool>();

var host = builder.Build();

try
{
    // No startup logging - MCP protocol requires clean stdout
    await host.RunAsync();
}
catch (Exception ex)
{
    // Only log critical errors to stderr
    Console.Error.WriteLine($"CRITICAL ERROR: {ex.Message}");
    Environment.Exit(1);
}