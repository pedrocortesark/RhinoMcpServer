# Rhino MCP Server

A Model Context Protocol (MCP) server that connects Rhino 3D CAD software to AI workflows. This .NET-based bridge extracts geometry data, document information, and layer details from Rhino files, making them accessible to Claude, n8n, Make, and other MCP-compatible tools.

## How It Works

### Core Concept
The Rhino MCP Server acts as a translator between Rhino 3D's rich geometry data and AI tools that want to analyze and work with that data.

```
AI Client (Claude) ↔ MCP Protocol ↔ Rhino MCP Server ↔ RhinoCommon API ↔ Rhino 3D
```

### What It Does
1. **Connects to Rhino**: Either running Rhino instance or opens .3dm files headlessly
2. **Extracts Data**: Pulls geometry objects, layer info, document metadata
3. **Provides Tools**: Exposes 7 MCP tools for different operations
4. **Returns JSON**: Structured data that AI can understand and work with

## Features

- **Document Operations**: Get active document info, list open documents, open 3DM files
- **Geometry Extraction**: Retrieve all objects, filter by layer, get object properties
- **Layer Information**: Access layer hierarchy, properties, and object counts
- **Headless Mode**: Process files without UI for better performance
- **Real-time Integration**: Connect to running Rhino instance

## Prerequisites

- Windows (RhinoCommon requirement)
- .NET 8.0 or later
- Rhino 3D installed (for live integration)

## Installation & Setup

### 1. Build and Run the Server

```bash
# Clone this repository
git clone <repository-url>
cd RhinoMcpServer

# Build the project
dotnet build

# Run the MCP server
dotnet run
```

The server runs as a console application using **stdio transport** - it communicates through standard input/output streams.

### 2. Configuration

Configure the server using `appsettings.json`:

```json
{
  "RhinoMcpServer": {
    "MaxObjectsPerRequest": 10000,
    "EnableHeadlessMode": true,
    "TimeoutSeconds": 30,
    "SupportedFileExtensions": [".3dm"],
    "Features": {
      "EnableGeometryExport": true,
      "EnableMaterialInfo": true,
      "EnableUserDataExtraction": true,
      "EnableStatistics": true
    }
  }
}
```

## Available MCP Tools

### Document Operations

- `get_active_document` - Get information about the currently active Rhino document
- `list_open_documents` - List all currently open Rhino documents  
- `open_document` - Open a Rhino 3DM file (headless mode if possible)
- `get_document_info` - Get detailed document information with statistics

### Geometry Extraction

- `get_all_objects` - Retrieve all geometry objects from the active document
- `get_objects_by_layer` - Get objects from a specific layer
- `get_layers` - Get all layers with hierarchy and properties

## Client Integration

### Claude Code Integration

Claude Code has built-in MCP support. Configure it to use this server:

```json
{
  "mcpServers": {
    "rhino": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/path/to/RhinoMcpServer"],
      "env": {}
    }
  }
}
```

Then use natural language queries:
- "Show me all objects in the current Rhino document"
- "Get geometry from the 'Buildings' layer"
- "What layers are available in this file?"

### n8n Workflow Integration

Use an **Execute Command** node:

```json
{
  "command": "dotnet run --project C:/path/to/RhinoMcpServer",
  "options": {
    "cwd": "C:/path/to/RhinoMcpServer"
  }
}
```

Send MCP protocol messages via stdin and read responses from stdout.

### Make.com Integration

Use Make's **System Command** module:
- **Command**: `dotnet run --project C:/path/to/RhinoMcpServer`
- **Input**: MCP protocol JSON messages
- **Output**: Parse JSON responses

### Custom Applications

Any application can consume this server by:

1. **Spawning the process**:
   ```csharp
   var process = new Process()
   {
       StartInfo = new ProcessStartInfo
       {
           FileName = "dotnet",
           Arguments = "run --project C:/path/to/RhinoMcpServer",
           UseShellExecute = false,
           RedirectStandardInput = true,
           RedirectStandardOutput = true
       }
   };
   process.Start();
   ```

2. **Sending MCP messages**:
   ```json
   {
     "jsonrpc": "2.0",
     "method": "tools/call",
     "params": {
       "name": "get_active_document",
       "arguments": {}
     },
     "id": 1
   }
   ```

3. **Receiving structured responses** with geometry data, layer information, and document metadata

### Direct Testing

Test the server manually:
```bash
# Start server
dotnet run

# In another terminal, send MCP messages
echo '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"get_active_document","arguments":{}},"id":1}' | dotnet run
```

## Architecture

### High-Level Structure
- **Program.cs**: Main entry point and MCP server configuration  
- **Services/RhinoService.cs**: Core Rhino API integration
- **Models/**: Data transfer objects for geometry and document info
- **Tools/**: MCP tool implementations

### Design Pattern
- **Host-based Architecture**: Uses Microsoft.Extensions.Hosting for dependency injection
- **Tool-based API**: Each operation is a separate McpServerTool
- **Service Layer**: RhinoService handles all RhinoCommon API interactions
- **Model Mapping**: Custom models provide JSON-serializable representations

## Use Cases

- **Design Analysis**: AI analyzes 3D models for optimization
- **Automated Reports**: Generate summaries of CAD files  
- **Batch Processing**: Process multiple .3dm files automatically
- **Quality Assurance**: Check models for standards compliance
- **Data Migration**: Extract geometry data for other systems

## Error Handling

The server includes comprehensive error handling and logging. Check the console output for detailed information about operations and any issues.

## Limitations

- Windows only (RhinoCommon requirement)
- Requires Rhino installation for full functionality
- Some features require active Rhino session
- Headless mode has limitations on document enumeration

## Development Notes

This project demonstrates how to:
- Build MCP servers in .NET
- Integrate with CAD applications via APIs  
- Handle complex geometry data structures
- Provide structured data to AI workflows

The server can be extended with additional tools for material extraction, rendering properties, custom object data, and more.