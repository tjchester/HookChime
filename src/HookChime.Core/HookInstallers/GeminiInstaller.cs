using System.Text.Json.Nodes;

namespace HookChime.Core.HookInstallers;

/// <summary>Gemini CLI: ~/.gemini/settings.json, hooks.AfterAgent[].hooks[].command.</summary>
public sealed class GeminiInstaller(string? homeDirectory = null) : IAgentInstaller
{
    public string AgentName => "gemini";

    private string HomeDirectory => homeDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private string ConfigPath => Path.Combine(HomeDirectory, ".gemini", "settings.json");

    public bool IsDetected() => Directory.Exists(Path.Combine(HomeDirectory, ".gemini"));

    public bool IsInstalled()
    {
        var array = LoadAfterAgentArray(out _);
        return array is not null && JsonHookUtils.ContainsHookChimeEntry(array);
    }

    public bool Install(string exePath)
    {
        var root = LoadRoot();
        var hooks = (JsonObject?)root["hooks"] ?? new JsonObject();
        hooks["enabled"] = true;
        var afterAgent = (JsonArray?)hooks["AfterAgent"] ?? new JsonArray();

        if (JsonHookUtils.ContainsHookChimeEntry(afterAgent)) return true; // already installed

        var quoted = ConfigFileUtils.PosixQuote(exePath);
        var innerHook = new JsonObject
        {
            ["type"] = "command",
            ["name"] = "hookchime-notification",
            ["command"] = $"{quoted} \"Gemini finished\" -t \"Gemini\"",
        };
        var hookItem = new JsonObject
        {
            ["matcher"] = "*",
            ["hooks"] = new JsonArray(innerHook),
        };

        afterAgent.Add(hookItem);
        hooks["AfterAgent"] = afterAgent;
        root["hooks"] = hooks;

        ConfigFileUtils.Backup(ConfigPath);
        return ConfigFileUtils.Write(ConfigPath, root.ToJsonString(JsonOptions.Indented));
    }

    public bool Uninstall()
    {
        var array = LoadAfterAgentArray(out var root);
        if (root is null || array is null) return true;

        var hooks = (JsonObject)root["hooks"]!;
        hooks["AfterAgent"] = JsonHookUtils.RemoveHookChimeEntries(array);

        ConfigFileUtils.Backup(ConfigPath);
        return ConfigFileUtils.Write(ConfigPath, root.ToJsonString(JsonOptions.Indented));
    }

    private JsonObject LoadRoot()
    {
        var text = ConfigFileUtils.ReadIfExists(ConfigPath);
        if (string.IsNullOrEmpty(text)) return new JsonObject();
        try { return JsonNode.Parse(text) as JsonObject ?? new JsonObject(); }
        catch { return new JsonObject(); }
    }

    private JsonArray? LoadAfterAgentArray(out JsonObject? root)
    {
        root = null;
        var text = ConfigFileUtils.ReadIfExists(ConfigPath);
        if (string.IsNullOrEmpty(text)) return null;

        JsonObject? parsed;
        try { parsed = JsonNode.Parse(text) as JsonObject; }
        catch { return null; }

        root = parsed;
        return (parsed?["hooks"] as JsonObject)?["AfterAgent"] as JsonArray;
    }
}
