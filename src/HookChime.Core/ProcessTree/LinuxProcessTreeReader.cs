using System.Runtime.Versioning;

namespace HookChime.Core.ProcessTree;

/// <summary>Walks the process tree via /proc/[pid]/stat — no native calls or external tools needed.</summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxProcessTreeReader : IProcessTreeReader
{
    public IEnumerable<ProcessInfo> WalkAncestors(int startPid)
    {
        var currentPid = startPid;
        for (var depth = 0; depth < 20; depth++)
        {
            var parentPid = TryReadParentPid(currentPid);
            if (parentPid is null || parentPid == 0 || parentPid == currentPid) yield break;

            var name = TryReadComm(parentPid.Value) ?? "";
            var cmdLine = TryReadCmdline(parentPid.Value);
            yield return new ProcessInfo(parentPid.Value, name, cmdLine);

            currentPid = parentPid.Value;
        }
    }

    private static int? TryReadParentPid(int pid)
    {
        try
        {
            var stat = File.ReadAllText($"/proc/{pid}/stat");
            // Format: "pid (comm) state ppid ...". The comm field is parenthesized and may
            // itself contain spaces/parens, so find the LAST ')' before splitting on spaces.
            var closeParen = stat.LastIndexOf(')');
            if (closeParen < 0) return null;
            var rest = stat[(closeParen + 1)..].TrimStart().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // rest[0] = state, rest[1] = ppid
            if (rest.Length < 2) return null;
            return int.TryParse(rest[1], out var ppid) ? ppid : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadComm(int pid)
    {
        try
        {
            return File.ReadAllText($"/proc/{pid}/comm").Trim();
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadCmdline(int pid)
    {
        try
        {
            var raw = File.ReadAllText($"/proc/{pid}/cmdline");
            return raw.Replace('\0', ' ').Trim();
        }
        catch
        {
            return null;
        }
    }
}
