using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;

namespace HookChime.Notifications.MacOS;

/// <summary>
/// Shells out to `osascript -e 'display notification'`, which ships with every macOS
/// install — no dependency to install. Known limitation: this API has no custom-icon
/// support (always shows Script Editor's icon) and no click-action callback; see the
/// capability matrix in the project README.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MacNotifier : INotifier
{
    public Task<bool> NotifyAsync(NotificationRequest request, CancellationToken ct = default)
    {
        try
        {
            var startInfo = new ProcessStartInfo { FileName = "osascript", UseShellExecute = false };
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add(BuildScript(request));

            using var process = Process.Start(startInfo);
            if (process is null) return Task.FromResult(false);
            process.WaitForExit(5000);
            return Task.FromResult(process.ExitCode == 0);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public string DescribeDryRun(NotificationRequest request) =>
        $"osascript -e {BuildScript(request)}";

    private static string BuildScript(NotificationRequest request) =>
        $"display notification {AppleScriptQuote(request.Message)} with title {AppleScriptQuote(request.Title)}";

    private static string AppleScriptQuote(string value)
    {
        var sb = new StringBuilder("\"");
        foreach (var c in value)
        {
            if (c is '"' or '\\') sb.Append('\\');
            sb.Append(c);
        }
        sb.Append('"');
        return sb.ToString();
    }
}
