using HookChime.Core.ProcessTree;

namespace HookChime.Core;

/// <summary>Auto-detects which coding agent invoked HookChime by walking the parent-process chain.</summary>
public static class AgentDetector
{
    private static readonly (string Needle, string Preset)[] CommandLinePatterns =
    [
        ("gemini-cli", "gemini"),
        ("gemini/cli", "gemini"),
        ("gemini\\cli", "gemini"),
        ("@google/gemini", "gemini"),
        ("@google\\gemini", "gemini"),
        ("claude-code", "claude"),
        ("@anthropic", "claude"),
        ("cursor", "cursor"),
    ];

    public static AppPreset? Detect(IProcessTreeReader reader, int startPid)
    {
        foreach (var process in reader.WalkAncestors(startPid))
        {
            var byName = AppPresets.Find(process.Name);
            if (byName is not null) return byName;

            if (process.CommandLine is { Length: > 0 } cmdLine)
            {
                var lower = cmdLine.ToLowerInvariant();
                foreach (var (needle, presetName) in CommandLinePatterns)
                {
                    if (lower.Contains(needle))
                    {
                        var preset = AppPresets.Find(presetName);
                        if (preset is not null) return preset;
                    }
                }
            }
        }
        return null;
    }
}
