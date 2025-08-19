using RhinoMcpServer.Models;
using Rhino;

namespace RhinoMcpServer.Services;

public interface IRhinoService
{
    Task<DocumentInfo?> GetActiveDocumentInfoAsync();
    Task<List<DocumentInfo>> GetOpenDocumentsAsync();
    Task<DocumentInfo?> OpenDocumentAsync(string filePath);
    Task<List<Models.RhinoObject>> GetAllObjectsAsync();
    Task<List<Models.RhinoObject>> GetObjectsByLayerAsync(string layerName);
    Task<List<RhinoLayer>> GetLayersAsync();
    Task<bool> IsRhinoRunningAsync();
    RhinoDoc? GetActiveDocument();
    RhinoDoc? OpenDocument(string filePath);
}