using System.Diagnostics;
using System.Runtime.Versioning;

namespace HookChime.Core.ProcessTree;

/// <summary>
/// Walks the process tree via `ps` (macOS has no /proc). One extra process spawn per
/// hop is fine here — this runs at most ~20 times, once per notification.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MacProcessTreeReader : IProcessTreeReader
{
    public IEnumerable<ProcessInfo> WalkAncestors(int startPid)
    {
        var currentPid = startPid;
        for (var depth = 0; depth < 20; depth++)
        {
            var (ppid, comm) = QueryProcess(currentPid);
            if (ppid is null || ppid == 0 || ppid == currentPid) yield break;

            var name = QueryProcess(ppid.Value).Comm ?? "";
            var cmdLine = QueryCommandLine(ppid.Value);
            yield return new ProcessInfo(ppid.Value, Path.GetFileName(name), cmdLine);

            currentPid = ppid.Value;
        }
    }

    private static (int? Ppid, string? Comm) QueryProcess(int pid)
    {
        var output = RunPs($"-o ppid=,comm= -p {pid}");
        if (string.IsNullOrWhiteSpace(output)) return (null, null);

        var trimmed = output.Trim();
        var spaceIdx = trimmed.IndexOf(' ');
        if (spaceIdx < 0) return (null, null);

        var ppidPart = trimmed[..spaceIdx];
        var commPart = trimmed[(spaceIdx + 1)..].Trim();
        return int.TryParse(ppidPart, out var ppid) ? (ppid, commPart) : (null, commPart);
    }

    private static string? QueryCommandLine(int pid) => RunPs($"-o command= -p {pid}")?.Trim();

    private static string? RunPs(string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "ps",
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            });
            if (process is null) return null;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);
            return output;
        }
        catch
        {
            return null;
        }
    }
}
