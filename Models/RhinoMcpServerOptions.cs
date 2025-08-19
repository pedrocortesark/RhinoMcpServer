using System.ComponentModel.DataAnnotations;

namespace RhinoMcpServer.Models;

public class RhinoMcpServerOptions
{
    public const string ConfigSection = "RhinoMcpServer";

    [Range(1, 100000)]
    public int MaxObjectsPerRequest { get; set; } = 10000;

    public bool EnableHeadlessMode { get; set; } = true;

    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    public List<string> SupportedFileExtensions { get; set; } = new() { ".3dm", ".rhino" };

    public FeatureOptions Features { get; set; } = new();
}

public class FeatureOptions
{
    public bool EnableGeometryExport { get; set; } = true;
    public bool EnableMaterialInfo { get; set; } = true;
    public bool EnableUserDataExtraction { get; set; } = true;
    public bool EnableStatistics { get; set; } = true;
}