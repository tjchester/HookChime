namespace HookChime.Core;

/// <summary>A named notification preset for a known coding agent.</summary>
public sealed record AppPreset(string Name, string Title);

public static class AppPresets
{
    public static readonly IReadOnlyList<AppPreset> All =
    [
        new AppPreset("claude", "Claude Code"),
        new AppPreset("copilot", "GitHub Copilot"),
        new AppPreset("gemini", "Gemini"),
        new AppPreset("codex", "Codex"),
        new AppPreset("cursor", "Cursor"),
    ];

    public static AppPreset? Find(string name) =>
        All.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
}
