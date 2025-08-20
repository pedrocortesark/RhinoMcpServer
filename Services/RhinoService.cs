using Microsoft.Extensions.Logging;
using Rhino;
using Rhino.DocObjects;
using RhinoMcpServer.Models;
using System.Drawing;
using RhinoDocObject = Rhino.DocObjects.RhinoObject;
using RhinoModelObject = RhinoMcpServer.Models.RhinoObject;

namespace RhinoMcpServer.Services;

public class RhinoService : IRhinoService
{
    private readonly ILogger<RhinoService> _logger;

    public RhinoService(ILogger<RhinoService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsRhinoRunningAsync()
    {
        return await Task.FromResult(RhinoDoc.ActiveDoc != null);
    }

    public RhinoDoc? GetActiveDocument()
    {
        try
        {
            return RhinoDoc.ActiveDoc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active document");
            return null;
        }
    }

    public RhinoDoc? OpenDocument(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File does not exist: {FilePath}", filePath);
                return null;
            }

            // Try to open using standard method
            var opened = RhinoDoc.Open(filePath, out var wasAlreadyOpen);
            if (opened != null)
            {
                _logger.LogInformation("Successfully opened document: {FilePath}", filePath);
                return opened;
            }

            _logger.LogWarning("Failed to open document: {FilePath}", filePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening document: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<DocumentInfo?> GetActiveDocumentInfoAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var doc = GetActiveDocument();
                if (doc == null)
                {
                    _logger.LogWarning("No active document found");
                    return null;
                }

                return ConvertDocumentToInfo(doc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active document info");
                return null;
            }
        });
    }

    public async Task<List<DocumentInfo>> GetOpenDocumentsAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var documents = new List<DocumentInfo>();
                
                // Since we don't have access to all open documents in headless mode,
                // we'll just return the active document if available
                var activeDoc = GetActiveDocument();
                if (activeDoc != null)
                {
                    var info = ConvertDocumentToInfo(activeDoc);
                    if (info != null)
                    {
                        documents.Add(info);
                    }
                }

                _logger.LogInformation("Found {Count} open documents", documents.Count);
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting open documents");
                return new List<DocumentInfo>();
            }
        });
    }

    public async Task<DocumentInfo?> OpenDocumentAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                var doc = OpenDocument(filePath);
                if (doc == null)
                {
                    return null;
                }

                return ConvertDocumentToInfo(doc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening document async: {FilePath}", filePath);
                return null;
            }
        });
    }

    public async Task<List<RhinoModelObject>> GetAllObjectsAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var doc = GetActiveDocument();
                if (doc == null)
                {
                    _logger.LogWarning("No active document found for getting objects");
                    return new List<RhinoModelObject>();
                }

                var objects = new List<RhinoModelObject>();
                var rhinoObjects = doc.Objects.GetObjectList(ObjectType.AnyObject);

                foreach (var obj in rhinoObjects)
                {
                    var rhinoObj = ConvertRhinoObject(obj);
                    if (rhinoObj != null)
                    {
                        objects.Add(rhinoObj);
                    }
                }

                _logger.LogInformation("Retrieved {Count} objects", objects.Count);
                return objects;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all objects");
                return new List<RhinoModelObject>();
            }
        });
    }

    public async Task<List<RhinoModelObject>> GetObjectsByLayerAsync(string layerName)
    {
        return await Task.Run(() =>
        {
            try
            {
                var doc = GetActiveDocument();
                if (doc == null)
                {
                    _logger.LogWarning("No active document found for getting objects by layer");
                    return new List<RhinoModelObject>();
                }

                var layer = doc.Layers.FindName(layerName);
                if (layer == null)
                {
                    _logger.LogWarning("Layer not found: {LayerName}", layerName);
                    return new List<RhinoModelObject>();
                }

                var objects = new List<RhinoModelObject>();
                var rhinoObjects = doc.Objects.FindByLayer(layer);

                foreach (var obj in rhinoObjects)
                {
                    var rhinoObj = ConvertRhinoObject(obj);
                    if (rhinoObj != null)
                    {
                        objects.Add(rhinoObj);
                    }
                }

                _logger.LogInformation("Retrieved {Count} objects from layer {LayerName}", objects.Count, layerName);
                return objects;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting objects by layer: {LayerName}", layerName);
                return new List<RhinoModelObject>();
            }
        });
    }

    public async Task<List<RhinoLayer>> GetLayersAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var doc = GetActiveDocument();
                if (doc == null)
                {
                    _logger.LogWarning("No active document found for getting layers");
                    return new List<RhinoLayer>();
                }

                var layers = new List<RhinoLayer>();
                var layerTable = doc.Layers;

                for (int i = 0; i < layerTable.Count; i++)
                {
                    var layer = layerTable[i];
                    if (layer != null)
                    {
                        var rhinoLayer = ConvertLayer(layer, doc);
                        if (rhinoLayer != null)
                        {
                            layers.Add(rhinoLayer);
                        }
                    }
                }

                _logger.LogInformation("Retrieved {Count} layers", layers.Count);
                return layers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting layers");
                return new List<RhinoLayer>();
            }
        });
    }

    private DocumentInfo? ConvertDocumentToInfo(RhinoDoc doc)
    {
        try
        {
            if (doc == null) return null;

            var layerCount = doc.Layers?.Count ?? 0;
            var objectCount = doc.Objects?.Count ?? 0;

            return new DocumentInfo
            {
                Name = doc.Name ?? "Untitled",
                Path = doc.Path ?? string.Empty,
                Units = doc.ModelUnitSystem.ToString(),
                IsModified = doc.Modified,
                CreatedDate = File.Exists(doc.Path) ? File.GetCreationTime(doc.Path) : null,
                ModifiedDate = File.Exists(doc.Path) ? File.GetLastWriteTime(doc.Path) : null,
                Notes = doc.Notes ?? string.Empty,
                Application = "Rhino 3D",
                LayerCount = layerCount,
                ObjectCount = objectCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting document to info");
            return null;
        }
    }

    private RhinoModelObject? ConvertRhinoObject(RhinoDocObject obj)
    {
        try
        {
            if (obj == null) return null;

            var attributes = obj.Attributes;
            var geometry = obj.Geometry;

            return new RhinoModelObject
            {
                Id = obj.Id,
                ObjectType = obj.ObjectType.ToString(),
                GeometryType = geometry?.ObjectType.ToString() ?? "Unknown",
                LayerId = attributes?.LayerIndex >= 0 ? obj.Document.Layers[attributes.LayerIndex].Id : Guid.Empty,
                LayerName = attributes?.LayerIndex >= 0 ? obj.Document.Layers[attributes.LayerIndex].Name : "Unknown",
                Name = attributes?.Name ?? string.Empty,
                IsVisible = attributes?.Visible ?? true,
                IsLocked = attributes?.Mode == ObjectMode.Locked,
                IsSelected = obj.IsSelected(false) > 0,
                BoundingBox = ConvertBoundingBox(geometry?.GetBoundingBox(true)),
                UserData = ExtractUserData(obj),
                Attributes = ConvertObjectAttributes(attributes)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting Rhino object");
            return null;
        }
    }

    private RhinoLayer? ConvertLayer(Layer layer, RhinoDoc doc)
    {
        try
        {
            if (layer == null) return null;

            var objectsOnLayer = doc.Objects.FindByLayer(layer);
            
            return new RhinoLayer
            {
                Id = layer.Id,
                Name = layer.Name,
                FullPath = layer.FullPath,
                Color = ConvertColor(layer.Color),
                IsVisible = layer.IsVisible,
                IsLocked = layer.IsLocked,
                ParentId = layer.ParentLayerId != Guid.Empty ? layer.ParentLayerId : null,
                ObjectCount = objectsOnLayer?.Length ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting layer");
            return null;
        }
    }

    private RhinoBoundingBox? ConvertBoundingBox(Rhino.Geometry.BoundingBox? bbox)
    {
        if (bbox == null || !bbox.Value.IsValid) return null;

        var box = bbox.Value;
        return new RhinoBoundingBox
        {
            Min = new RhinoPoint3d { X = box.Min.X, Y = box.Min.Y, Z = box.Min.Z },
            Max = new RhinoPoint3d { X = box.Max.X, Y = box.Max.Y, Z = box.Max.Z },
            IsValid = box.IsValid
        };
    }

    private RhinoColor ConvertColor(Color color)
    {
        return new RhinoColor
        {
            R = color.R,
            G = color.G,
            B = color.B,
            Name = color.Name
        };
    }

    private Dictionary<string, string> ExtractUserData(RhinoDocObject obj)
    {
        var userData = new Dictionary<string, string>();
        
        try
        {
            // Extract basic user data - this is a simplified implementation
            if (obj.Attributes?.UserData != null)
            {
                foreach (var item in obj.Attributes.UserData)
                {
                    if (item != null)
                    {
                        userData[item.GetType().Name] = item.ToString() ?? string.Empty;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract user data");
        }

        return userData;
    }

    private RhinoObjectAttributes ConvertObjectAttributes(ObjectAttributes? attributes)
    {
        var result = new RhinoObjectAttributes();
        
        if (attributes != null)
        {
            result.DisplayColor = attributes.ObjectColor != Color.Empty ? ConvertColor(attributes.ObjectColor) : null;
            result.PlotColor = attributes.PlotColor != Color.Empty ? ConvertColor(attributes.PlotColor) : null;
            result.PlotWeight = attributes.PlotWeight;
            result.Linetype = attributes.LinetypeSource.ToString();
        }

        return result;
    }
}