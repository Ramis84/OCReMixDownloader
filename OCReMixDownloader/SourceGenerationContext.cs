using System.Text.Json.Serialization;

namespace OCReMixDownloader;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SettingsModel))]
internal partial class SourceGenerationContext : JsonSerializerContext;