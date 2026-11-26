using System.Text.Json.Serialization;
using Com.MiniCalculator.Models;

namespace Com.MiniCalculator.Services;

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(SettingsData))]
[JsonSerializable(typeof(List<HistoryEntry>))]
internal sealed partial class AppJsonContext : JsonSerializerContext
{
}
