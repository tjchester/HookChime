using System.Text.Json;

namespace HookChime.Core.HookInstallers;

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };
}
