using System.Reflection;

namespace HookChime.Core;

/// <summary>
/// HookChime's own bell icon, embedded in the assembly and extracted to a temp file on
/// demand — notifiers need a real file path (Windows toast XML, notify-send -i), not
/// embedded bytes. The artwork is from Tabler Icons (MIT); see /NOTICE.md.
/// </summary>
public static class DefaultIcon
{
    private const string ResourceName = "HookChime.Core.Assets.hookchime.png";

    private static string? _cachedPath;

    public static string? ExtractToTempFile()
    {
        if (_cachedPath is not null && File.Exists(_cachedPath)) return _cachedPath;

        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
            if (stream is null) return null;

            var path = Path.Combine(Path.GetTempPath(), "hookchime_icon.png");
            using (var file = File.Create(path))
            {
                stream.CopyTo(file);
            }

            _cachedPath = path;
            return path;
        }
        catch
        {
            return null;
        }
    }
}
