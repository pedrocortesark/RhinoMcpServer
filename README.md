# Rhino MCP Server

A Model Context Protocol (MCP) server that connects to Rhino 3D and extracts information from open files. This server can be used with Claude, n8n, Make, and other workflow tools that support MCP.

## Features


## Prerequisites


## Installation

1. Clone this repository
2. Build the project:
   ```bash
   dotnet build
   ```
3. Run the server:
   ```bash
   dotnet run
   ```

## Configuration

Configure the server using `appsettings.json`:

```json
{
  "RhinoMcpServer": {
    "MaxObjectsPerRequest": 10000,
    "EnableHeadlessMode": true,
    "TimeoutSeconds": 30,
    "SupportedFileExtensions": [".3dm", ".rhino"]
  }
}
```

## MCP Tools

### Document Operations


### Geometry Extraction


## Usage with Claude

1. Start the Rhino MCP Server
2. Configure Claude to use the server via stdio transport
3. Use the available tools to extract Rhino data

Example queries:

## Usage with n8n/Make

The server uses stdio transport and can be integrated into workflows using command-line execution nodes.

## Architecture


## Error Handling

The server includes comprehensive error handling and logging. Check the console output for detailed information about operations and any issues.

## Limitations


## Development Notes

This project demonstrates how to:

The server can be extended with additional tools for material extraction, rendering properties, custom object data, and more.
=======
# RhinoMcpServer
Official repository for the Rhino MCP Server project. This project provides a robust server implementation for integrating Rhino with the Model Context Protocol (MCP). It includes source code, models, services, and tools to enable seamless communication and automation between Rhino and external applications.
>>>>>>> 4750f61341a9b857fd48b7341411e9c10a076a90
# Rhino MCP Server

A Model Context Protocol (MCP) server that connects to Rhino 3D and extracts information from open files. This server can be used with Claude, n8n, Make, and other workflow tools that support MCP.

## Features

- **Document Operations**: Get active document info, list open documents, open 3DM files
- **Geometry Extraction**: Retrieve all objects, filter by layer, get object properties
- **Layer Information**: Access layer hierarchy, properties, and object counts
- **Headless Mode**: Process files without UI for better performance
- **Real-time Integration**: Connect to running Rhino instance

## Prerequisites

- .NET 8.0 or later
- Rhino 3D (for live integration)
- Windows (Rhino requirement)

## Installation

1. Clone this repository
2. Build the project:
   ```bash
   dotnet build
   ```
3. Run the server:
   ```bash
   dotnet run
   ```

## Configuration

Configure the server using `appsettings.json`:

```json
{
  "RhinoMcpServer": {
    "MaxObjectsPerRequest": 10000,
    "EnableHeadlessMode": true,
    "TimeoutSeconds": 30,
    "SupportedFileExtensions": [".3dm", ".rhino"]
  }
}
```

## MCP Tools

### Document Operations

- `get_active_document` - Get information about the currently active Rhino document
- `list_open_documents` - List all currently open Rhino documents
- `open_document` - Open a Rhino 3DM file (headless mode if possible)
- `get_document_info` - Get detailed document information with statistics

### Geometry Extraction

- `get_all_objects` - Retrieve all geometry objects from the active document
- `get_objects_by_layer` - Get objects from a specific layer
- `get_layers` - Get all layers with hierarchy and properties

## Usage with Claude

1. Start the Rhino MCP Server
2. Configure Claude to use the server via stdio transport
3. Use the available tools to extract Rhino data

Example queries:
- "Show me all objects in the current Rhino document"
- "Get objects from the 'Buildings' layer"
- "What layers are available in this file?"

## Usage with n8n/Make

The server uses stdio transport and can be integrated into workflows using command-line execution nodes.

## Architecture

- **Program.cs**: Main entry point and MCP server configuration
- **Services/RhinoService.cs**: Core Rhino API integration
- **Models/**: Data transfer objects for geometry and document info
- **Tools/**: MCP tool implementations

## Error Handling

The server includes comprehensive error handling and logging. Check the console output for detailed information about operations and any issues.

## Limitations

- Windows only (Rhino requirement)
- Requires Rhino installation for full functionality
- Some features require active Rhino session

## Development Notes

This project demonstrates how to:
- Build MCP servers in .NET
- Integrate with CAD applications via APIs
- Handle complex geometry data structures
- Provide structured data to AI workflows

The server can be extended with additional tools for material extraction, rendering properties, custom object data, and more.
