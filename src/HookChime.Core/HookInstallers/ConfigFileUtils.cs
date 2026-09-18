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

    /// <summary>Wraps an absolute exe path in quotes only if it contains a space, matching shell quoting rules.</summary>
    public static string QuoteIfNeeded(string exePath) =>
        exePath.Contains(' ') ? $"\"{exePath}\"" : exePath;
}
