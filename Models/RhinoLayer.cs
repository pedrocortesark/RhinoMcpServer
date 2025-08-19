using System.Text.Json.Serialization;

namespace RhinoMcpServer.Models;

public class RhinoLayer
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("fullPath")]
    public string FullPath { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public RhinoColor Color { get; set; } = new();

    [JsonPropertyName("isVisible")]
    public bool IsVisible { get; set; }

    [JsonPropertyName("isLocked")]
    public bool IsLocked { get; set; }

    [JsonPropertyName("parentId")]
    public Guid? ParentId { get; set; }

    [JsonPropertyName("childrenIds")]
    public List<Guid> ChildrenIds { get; set; } = new();

    [JsonPropertyName("objectCount")]
    public int ObjectCount { get; set; }
}

public class RhinoColor
{
    [JsonPropertyName("r")]
    public int R { get; set; }

    [JsonPropertyName("g")]
    public int G { get; set; }

    [JsonPropertyName("b")]
    public int B { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}