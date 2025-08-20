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

// Configure logging to stderr only (stdout is reserved for MCP protocol)
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);

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
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var options = host.Services.GetRequiredService<IOptions<RhinoMcpServerOptions>>();
    
    // Startup logging moved to stderr to avoid interfering with MCP protocol on stdout
    logger.LogDebug("Starting Rhino MCP Server v{Version}...", version);
    logger.LogDebug("Configuration: MaxObjects={MaxObjects}, HeadlessMode={HeadlessMode}, Timeout={Timeout}s",
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