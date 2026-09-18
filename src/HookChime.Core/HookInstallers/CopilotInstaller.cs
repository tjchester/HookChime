using System.Text.Json.Nodes;

namespace HookChime.Core.HookInstallers;

/// <summary>
/// GitHub Copilot CLI: a dedicated .github/hooks/hookchime.json file (repo-relative,
/// unlike the other three agents which use a per-user config).
/// </summary>
public sealed class CopilotInstaller(string? workingDirectory = null) : IAgentInstaller
{
    public string AgentName => "copilot";

    private string WorkingDirectory => workingDirectory ?? Directory.GetCurrentDirectory();

    private string ConfigPath => Path.Combine(WorkingDirectory, ".github", "hooks", "hookchime.json");

    public bool IsDetected() => Directory.Exists(Path.Combine(WorkingDirectory, ".github"));

    public bool IsInstalled()
    {
        var array = LoadSessionEndArray(out _);
        return array is not null && JsonHookUtils.ContainsHookChimeEntry(array);
    }

    public bool Install(string exePath)
    {
        var root = LoadRoot();
        root["version"] = 1;
        var hooks = (JsonObject?)root["hooks"] ?? new JsonObject();
        var sessionEnd = (JsonArray?)hooks["sessionEnd"] ?? new JsonArray();

        if (JsonHookUtils.ContainsHookChimeEntry(sessionEnd)) return true; // already installed

        // No manual backslash-doubling here: System.Text.Json JSON-escapes this string
        // automatically on write, and PowerShell (unlike bash) doesn't treat backslash
        // as an escape character, so the path only needs simple quoting.
        var quotedPath = ConfigFileUtils.WindowsQuote(exePath);
        var hookObj = new JsonObject
        {
            ["type"] = "command",
            ["bash"] = "hookchime 'Copilot finished' -t 'GitHub Copilot'",
            ["powershell"] = $"{quotedPath} 'Copilot finished' -t 'GitHub Copilot'",
            ["timeoutSec"] = 5,
        };

        sessionEnd.Add(hookObj);
        hooks["sessionEnd"] = sessionEnd;
        root["hooks"] = hooks;

        ConfigFileUtils.Backup(ConfigPath);
        return ConfigFileUtils.Write(ConfigPath, root.ToJsonString(JsonOptions.Indented));
    }

    public bool Uninstall()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                ConfigFileUtils.Backup(ConfigPath);
                File.Delete(ConfigPath);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private JsonObject LoadRoot()
    {
        var text = ConfigFileUtils.ReadIfExists(ConfigPath);
        if (string.IsNullOrEmpty(text)) return new JsonObject();
        try { return JsonNode.Parse(text) as JsonObject ?? new JsonObject(); }
        catch { return new JsonObject(); }
    }

    private JsonArray? LoadSessionEndArray(out JsonObject? root)
    {
        root = null;
        var text = ConfigFileUtils.ReadIfExists(ConfigPath);
        if (string.IsNullOrEmpty(text)) return null;

        JsonObject? parsed;
        try { parsed = JsonNode.Parse(text) as JsonObject; }
        catch { return null; }

        root = parsed;
        return (parsed?["hooks"] as JsonObject)?["sessionEnd"] as JsonArray;
    }
}
