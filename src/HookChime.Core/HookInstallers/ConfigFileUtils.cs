namespace HookChime.Core.HookInstallers;

/// <summary>Small file helpers shared by every agent installer: backup-before-write, best-effort I/O.</summary>
internal static class ConfigFileUtils
{
    public static string? ReadIfExists(string path) => File.Exists(path) ? File.ReadAllText(path) : null;

    public static bool Backup(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Copy(path, path + ".bak", overwrite: true);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool Write(string path, string content)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Quotes a path for a command line that will be parsed by a POSIX shell (bash/sh) —
    /// which is how Claude Code and Gemini CLI invoke hook commands, even on Windows
    /// (e.g. via Git Bash). Unquoted, bash treats backslash as an escape character and
    /// silently strips it before the next character, mangling any Windows path
    /// (C:\Users\... -> CUsers...) whether or not it contains spaces — quoting alone
    /// isn't enough either, since bash still un-escapes backslashes inside double quotes,
    /// so each backslash must also be doubled.
    /// </summary>
    public static string PosixQuote(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("$", "\\$")
            .Replace("`", "\\`");
        return $"\"{escaped}\"";
    }

    /// <summary>
    /// Quotes a path for a command line that PowerShell will parse (e.g. Copilot's
    /// "powershell" hook variant). PowerShell's escape character is backtick, not
    /// backslash, so — unlike PosixQuote — backslashes must NOT be doubled here.
    /// </summary>
    public static string WindowsQuote(string value) => $"\"{value}\"";
}
