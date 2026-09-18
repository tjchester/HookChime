using System.Text.Json.Nodes;

namespace HookChime.Core.HookInstallers;

/// <summary>Claude Code: ~/.claude/settings.json, hooks.Stop[].hooks[].command.</summary>
public sealed class ClaudeInstaller(string? homeDirectory = null) : IAgentInstaller
{
    public string AgentName => "claude";

    private string HomeDirectory => homeDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private string ConfigPath => Path.Combine(HomeDirectory, ".claude", "settings.json");

    public bool IsDetected() => Directory.Exists(Path.Combine(HomeDirectory, ".claude"));

    public bool IsInstalled()
    {
        var stopArray = LoadStopArray(out _);
        return stopArray is not null && JsonHookUtils.ContainsHookChimeEntry(stopArray);
    }

    public bool Install(string exePath)
    {
        var root = LoadRoot();
        var hooks = (JsonObject?)root["hooks"] ?? new JsonObject();
        var stop = (JsonArray?)hooks["Stop"] ?? new JsonArray();

        if (JsonHookUtils.ContainsHookChimeEntry(stop)) return true; // already installed

        var quoted = ConfigFileUtils.QuoteIfNeeded(exePath);
        var innerHook = new JsonObject
        {
            ["type"] = "command",
            ["command"] = $"{quoted} \"Task complete\" -t \"Claude Code\"",
            ["timeout"] = 5000,
        };
        var hookItem = new JsonObject { ["hooks"] = new JsonArray(innerHook) };

        stop.Add(hookItem);
        hooks["Stop"] = stop;
        root["hooks"] = hooks;

        ConfigFileUtils.Backup(ConfigPath);
        return ConfigFileUtils.Write(ConfigPath, root.ToJsonString(JsonOptions.Indented));
    }

    public bool Uninstall()
    {
        var stopArray = LoadStopArray(out var root);
        if (root is null || stopArray is null) return true; // nothing to remove

        var hooks = (JsonObject)root["hooks"]!;
        hooks["Stop"] = JsonHookUtils.RemoveHookChimeEntries(stopArray);

        ConfigFileUtils.Backup(ConfigPath);
        return ConfigFileUtils.Write(ConfigPath, root.ToJsonString(JsonOptions.Indented));
    }

    private JsonObject LoadRoot()
    {
        var text = ConfigFileUtils.ReadIfExists(ConfigPath);
        if (string.IsNullOrEmpty(text)) return new JsonObject();
        try
        {
            return JsonNode.Parse(text) as JsonObject ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }

    private JsonArray? LoadStopArray(out JsonObject? root)
    {
        root = null;
        var text = ConfigFileUtils.ReadIfExists(ConfigPath);
        if (string.IsNullOrEmpty(text)) return null;

        JsonObject? parsed;
        try { parsed = JsonNode.Parse(text) as JsonObject; }
        catch { return null; }

        root = parsed;
        return (parsed?["hooks"] as JsonObject)?["Stop"] as JsonArray;
    }
}
