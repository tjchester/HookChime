namespace HookChime.Core;

public static class VersionComparer
{
    /// <summary>True if <paramref name="remote"/> is a newer dotted-numeric version than <paramref name="current"/>.</summary>
    public static bool IsNewer(string current, string remote)
    {
        var c = ParseParts(current);
        var r = ParseParts(remote);
        var length = Math.Max(c.Count, r.Count);
        for (var i = 0; i < length; i++)
        {
            var cp = i < c.Count ? c[i] : 0;
            var rp = i < r.Count ? r[i] : 0;
            if (rp != cp) return rp > cp;
        }
        return false;
    }

    private static List<int> ParseParts(string version)
    {
        var parts = new List<int>();
        foreach (var segment in version.Split('.'))
        {
            var digits = new string(segment.TakeWhile(char.IsAsciiDigit).ToArray());
            parts.Add(int.TryParse(digits, out var n) ? n : 0);
        }
        return parts;
    }
}
