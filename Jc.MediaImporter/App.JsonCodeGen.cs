using System.Drawing;
using System.Text.Json.Serialization;

namespace Jc.MediaImporter;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    IgnoreReadOnlyFields = true,
    IgnoreReadOnlyProperties = true,
    Converters = [
        typeof(ColorConverter),
    ]
)]
[JsonSerializable(typeof(ViewModels.SettingsViewModel))]
internal partial class JsonCodeGen : JsonSerializerContext { }