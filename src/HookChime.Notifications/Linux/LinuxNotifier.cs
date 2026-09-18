using System.Diagnostics;
using System.Runtime.Versioning;

namespace HookChime.Notifications.Linux;

/// <summary>
/// Shells out to `notify-send` (part of libnotify, preinstalled on virtually every
/// desktop distro — GNOME, KDE, XFCE all ship a notification daemon that implements
/// it). No click-action support here; see the capability matrix in the README for
/// why that's a separate, harder piece of work (a persistent D-Bus listener) left
/// for later.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxNotifier : INotifier
{
    public async Task<bool> NotifyAsync(NotificationRequest request, CancellationToken ct = default)
    {
        if (!IsNotifySendAvailable()) return false;

        try
        {
            using var process = Process.Start(BuildStartInfo(request));
            if (process is null) return false;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public string DescribeDryRun(NotificationRequest request)
    {
        var startInfo = BuildStartInfo(request);
        var args = string.Join(' ', startInfo.ArgumentList);
        return $"notify-send {args}" + (IsNotifySendAvailable() ? "" : "  (notify-send not found on PATH — would silently skip)");
    }

    private static ProcessStartInfo BuildStartInfo(NotificationRequest request)
    {
        var startInfo = new ProcessStartInfo { FileName = "notify-send", UseShellExecute = false };
        if (!string.IsNullOrEmpty(request.IconPath))
        {
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(request.IconPath);
        }
        startInfo.ArgumentList.Add(request.Title);
        startInfo.ArgumentList.Add(request.Message);
        return startInfo;
    }

    private static bool IsNotifySendAvailable()
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        return pathVar.Split(Path.PathSeparator).Any(dir =>
        {
            try { return File.Exists(Path.Combine(dir, "notify-send")); }
            catch { return false; }
        });
    }
}
