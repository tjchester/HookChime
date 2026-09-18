using System.Text;

namespace HookChime.Core.HookInstallers;

/// <summary>
/// OpenAI Codex CLI: ~/.codex/config.toml, top-level `notify = [...]` key.
/// TOML has no first-class array-of-hooks concept here (unlike the other three
/// agents), so this edits the file as text — same line-scan approach as the
/// original C++ implementation, deliberately kept rather than pulling in a TOML
/// library, since the algorithm only ever touches a single top-level key and
/// must preserve the user's existing tables/comments exactly.
/// </summary>
public sealed class CodexInstaller(string? homeDirectory = null) : IAgentInstaller
{
    public string AgentName => "codex";

    private string HomeDirectory => homeDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private string ConfigDir => Path.Combine(HomeDirectory, ".codex");

    private string ConfigPath => Path.Combine(ConfigDir, "config.toml");

    public bool IsDetected() => Directory.Exists(ConfigDir);

    public bool IsInstalled()
    {
        var content = ConfigFileUtils.ReadIfExists(ConfigPath);
        if (string.IsNullOrEmpty(content)) return false;
        return FindTopLevelNotifyLine(content, out var line) &&
               line.Contains(Constants.ExeMarker, StringComparison.OrdinalIgnoreCase);
    }

    public bool Install(string exePath)
    {
        Directory.CreateDirectory(ConfigDir);

        var escapedPath = exePath.Replace("\\", "\\\\");
        var notifyLine = $"notify = [\"{escapedPath}\", \"Codex finished\", \"-t\", \"Codex\"]\n";

        var content = ConfigFileUtils.ReadIfExists(ConfigPath) ?? "";
        var (bom, body) = SplitBom(content);

        if (body.Length == 0)
        {
            return ConfigFileUtils.Write(ConfigPath, bom + notifyLine);
        }

        ConfigFileUtils.Backup(ConfigPath);

        body = RemoveHookChimeNotifyLines(body);

        var firstTablePos = FindFirstTablePos(body);
        var searchLimit = firstTablePos ?? body.Length;
        var (topStart, topEnd) = FindTopLevelNotifyRange(body, searchLimit);

        string result;
        if (topStart is not null)
        {
            result = body[..topStart.Value] + notifyLine + body[topEnd!.Value..];
        }
        else if (firstTablePos is not null)
        {
            result = body[..firstTablePos.Value] + notifyLine + body[firstTablePos.Value..];
        }
        else
        {
            var withTrailingNewline = body.Length > 0 && body[^1] != '\n' ? body + "\n" : body;
            result = withTrailingNewline + notifyLine;
        }

        return ConfigFileUtils.Write(ConfigPath, bom + result);
    }

    public bool Uninstall()
    {
        var content = ConfigFileUtils.ReadIfExists(ConfigPath);
        if (string.IsNullOrEmpty(content)) return true;

        ConfigFileUtils.Backup(ConfigPath);
        var (bom, body) = SplitBom(content);
        var cleaned = RemoveHookChimeNotifyLines(body);
        return ConfigFileUtils.Write(ConfigPath, bom + cleaned);
    }

    private static (string Bom, string Body) SplitBom(string content)
    {
        const string bom = "﻿";
        return content.StartsWith(bom, StringComparison.Ordinal)
            ? (bom, content[bom.Length..])
            : ("", content);
    }

    private static bool IsNotifyKeyLine(string trimmedLine)
    {
        const string key = "notify";
        if (!trimmedLine.StartsWith(key, StringComparison.Ordinal)) return false;
        var pos = key.Length;
        while (pos < trimmedLine.Length && (trimmedLine[pos] == ' ' || trimmedLine[pos] == '\t')) pos++;
        return pos < trimmedLine.Length && trimmedLine[pos] == '=';
    }

    private static bool FindTopLevelNotifyLine(string content, out string line)
    {
        foreach (var raw in content.Split('\n'))
        {
            var trimmed = raw.Trim(' ', '\t', '\r');
            if (IsNotifyKeyLine(trimmed))
            {
                line = raw;
                return true;
            }
            if (trimmed.StartsWith('[')) break; // reached first table; no top-level notify before it
        }
        line = "";
        return false;
    }

    private static string RemoveHookChimeNotifyLines(string body)
    {
        var sb = new StringBuilder(body.Length);
        var lines = body.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim(' ', '\t', '\r');
            var isHookChimeNotify = IsNotifyKeyLine(trimmed) &&
                trimmed.Contains(Constants.ExeMarker, StringComparison.OrdinalIgnoreCase);

            if (!isHookChimeNotify)
            {
                sb.Append(line);
                if (i < lines.Length - 1) sb.Append('\n');
            }
        }
        return sb.ToString();
    }

    private static int? FindFirstTablePos(string body)
    {
        var pos = 0;
        foreach (var line in body.Split('\n'))
        {
            var trimmed = line.TrimStart(' ', '\t', '\r');
            if (trimmed.StartsWith('[')) return pos;
            pos += line.Length + 1;
        }
        return null;
    }

    private static (int? Start, int? End) FindTopLevelNotifyRange(string body, int searchLimit)
    {
        var pos = 0;
        foreach (var line in body.Split('\n'))
        {
            if (pos >= searchLimit) break;
            var trimmed = line.Trim(' ', '\t', '\r');
            if (IsNotifyKeyLine(trimmed))
            {
                var lineEnd = pos + line.Length;
                var end = lineEnd < body.Length ? lineEnd + 1 : body.Length;
                return (pos, end);
            }
            pos += line.Length + 1;
        }
        return (null, null);
    }
}
