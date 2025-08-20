# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Building and Running
```bash
# Build the project
dotnet build

# Run the MCP server
dotnet run

# Run with specific configuration
dotnet run --configuration Release
```

### Testing and Debugging
```bash
# Check for build errors without running
dotnet build --verbosity normal

# Run in development mode with detailed logging
dotnet run --environment Development
```

## High-Level Architecture

### MCP Server Implementation
This is a .NET-based Model Context Protocol (MCP) server that integrates Rhino 3D CAD software with AI workflows. The server exposes Rhino document and geometry data through standardized MCP tools.

**Core Design Pattern:**
- **Host-based Architecture**: Uses Microsoft.Extensions.Hosting for dependency injection and service lifecycle
- **Tool-based API**: Implements McpServerTool base class for each operation
- **Service Layer**: RhinoService handles all RhinoCommon API interactions
- **Model Mapping**: Custom models (DocumentInfo, RhinoObject, RhinoLayer) provide JSON-serializable representations

### Key Components

**Program.cs**: 
- Configures MCP server with stdio transport
- Registers all tools as scoped services
- Sets up configuration binding and logging

**Services/RhinoService.cs**:
- Implements IRhinoService interface
- Handles RhinoDoc operations (active document, open files)
- Converts Rhino objects to custom models to avoid serialization issues
- Uses alias pattern: `RhinoDocObject = Rhino.DocObjects.RhinoObject` to resolve naming conflicts

**Tools/** (7 MCP Tools):
- Each inherits from McpServerTool
- Implements ProtocolTool property with schema using `JsonSerializer.SerializeToElement()`
- InvokeAsync method handles request/response with proper error handling
- Content returned as `List<ContentBlock>` with `TextContentBlock`

### MCP Tool Pattern
```csharp
public override Tool ProtocolTool => new()
{
    Name = "tool_name",
    Description = "Tool description",
    InputSchema = JsonSerializer.SerializeToElement(new { /* schema object */ })
};

public override async ValueTask<CallToolResult> InvokeAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
{
    // Extract arguments from request.Params.Arguments.TryGetValue()
    // Call service methods
    // Return CallToolResult with List<ContentBlock>
}
```

### RhinoCommon Integration Notes
- Uses RhinoDoc.ActiveDoc for active document access
- Document opening handled via RhinoDoc.Open() with error handling
- Object enumeration through doc.Objects.GetObjectList()
- Layer access via doc.Layers with hierarchy support
- All Rhino types converted to custom models for JSON serialization

### Configuration System
- appsettings.json with RhinoMcpServerOptions section
- Configurable limits (MaxObjectsPerRequest, TimeoutSeconds)
- Feature toggles for different capabilities
- Environment variable overrides with "RHINO_MCP_" prefix

### Error Handling Strategy
- Comprehensive try-catch in all tool methods
- Structured logging with correlation IDs
- CallToolResult.IsError for MCP error responses
- Graceful degradation when Rhino is not available

## Technical Constraints

### MCP SDK Version
Uses ModelContextProtocol 0.3.0-preview.3 which has specific patterns:
- Tools inherit McpServerTool (not interface-based)
- InputSchema requires JsonSerializer.SerializeToElement()
- Content must be List<ContentBlock> with TextContentBlock
- Arguments accessed via request.Params.Arguments dictionary

### RhinoCommon Dependencies
- Requires RhinoCommon 8.21+ NuGet package
- Some APIs work only when Rhino is running
- Headless mode has limitations on document enumeration
- File operations may require specific Rhino licensing

### Windows-Only Limitation
RhinoCommon is Windows-specific, so this server only runs on Windows platforms with Rhino installed.

## Configuration Notes

The server expects Rhino to be installed and accessible. When Rhino is not running, some operations will fail gracefully. The server can process .3dm files in headless mode for basic document information extraction.

For integration with Claude Code or other MCP clients, the server communicates via stdio transport and provides structured JSON responses for all geometry and document data.