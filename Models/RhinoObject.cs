using System.Text.Json.Serialization;

namespace RhinoMcpServer.Models;

public class RhinoObject
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("objectType")]
    public string ObjectType { get; set; } = string.Empty;

    [JsonPropertyName("geometryType")]
    public string GeometryType { get; set; } = string.Empty;

    [JsonPropertyName("layerId")]
    public Guid LayerId { get; set; }

    [JsonPropertyName("layerName")]
    public string LayerName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; set; }

    [JsonPropertyName("isLocked")]
    public bool IsLocked { get; set; }

    [JsonPropertyName("isSelected")]
    public bool IsSelected { get; set; }

    [JsonPropertyName("boundingBox")]
    public RhinoBoundingBox? BoundingBox { get; set; }

    [JsonPropertyName("material")]
    public RhinoMaterial? Material { get; set; }

    [JsonPropertyName("userData")]
    public Dictionary<string, string> UserData { get; set; } = new();

    [JsonPropertyName("attributes")]
    public RhinoObjectAttributes Attributes { get; set; } = new();
}

public class RhinoBoundingBox
{
    [JsonPropertyName("min")]
    public RhinoPoint3d Min { get; set; } = new();

    [JsonPropertyName("max")]
    public RhinoPoint3d Max { get; set; } = new();

    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }
}

public class RhinoPoint3d
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("z")]
    public double Z { get; set; }
}

public class RhinoMaterial
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("diffuseColor")]
    public RhinoColor DiffuseColor { get; set; } = new();

    [JsonPropertyName("reflectivity")]
    public double Reflectivity { get; set; }

    [JsonPropertyName("transparency")]
    public double Transparency { get; set; }
}

public class RhinoObjectAttributes
{
    [JsonPropertyName("displayColor")]
    public RhinoColor? DisplayColor { get; set; }

    [JsonPropertyName("plotColor")]
    public RhinoColor? PlotColor { get; set; }

    [JsonPropertyName("plotWeight")]
    public double PlotWeight { get; set; }

    [JsonPropertyName("linetype")]
    public string Linetype { get; set; } = string.Empty;
}