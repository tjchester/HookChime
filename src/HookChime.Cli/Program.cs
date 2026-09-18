using System.Runtime.InteropServices;
using HookChime.Core;
using HookChime.Core.HookInstallers;
using HookChime.Core.Ntfy;
using HookChime.Core.ProcessTree;
using HookChime.Core.UpdateCheck;
using HookChime.Notifications;

return await Cli.RunAsync(args);

internal static class Cli
{
    private static readonly IAgentInstaller[] Installers =
    [
        new ClaudeInstaller(),
        new GeminiInstaller(),
        new CopilotInstaller(),
        new CodexInstaller(),
    ];

    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 0;
        }

        string? message = null;
        var title = "Notification";
        string? iconPath = null;
        var explicitTitle = false;
        var doInstall = false;
        var doUninstall = false;
        var doStatus = false;
        var dryRun = false;
        var debug = false;
        string? installAgent = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h" or "--help":
                    PrintUsage();
                    return 0;
                case "-v" or "--version":
                    Console.WriteLine($"hookchime v{Constants.Version}");
                    return 0;
                case "--install":
                    doInstall = true;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                    {
                        installAgent = args[++i];
                    }
                    break;
                case "--uninstall":
                    doUninstall = true;
                    break;
                case "--status":
                    doStatus = true;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--debug":
                    debug = true;
                    break;
                case "-t" or "--title":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Error: --title requires an argument");
                        return 1;
                    }
                    title = args[++i];
                    explicitTitle = true;
                    break;
                case "--app":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Error: --app requires an argument");
                        return 1;
                    }
                    var appName = args[++i];
                    var preset = AppPresets.Find(appName);
                    if (preset is null)
                    {
                        Console.Error.WriteLine($"Error: Unknown app preset '{appName}'");
                        Console.Error.WriteLine("Available presets: " + string.Join(", ", AppPresets.All.Select(p => p.Name)));
                        return 1;
                    }
                    if (!explicitTitle) title = preset.Title;
                    break;
                case "-i" or "--icon":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("Error: --icon requires an argument");
                        return 1;
                    }
                    iconPath = Path.GetFullPath(args[++i]);
                    break;
                default:
                    if (!arg.StartsWith('-') && message is null)
                    {
                        message = arg;
                    }
                    break;
            }
        }

        if (doStatus)
        {
            ShowStatus();
            return 0;
        }

        if (doInstall)
        {
            return HandleInstall(installAgent);
        }

        if (doUninstall)
        {
            return HandleUninstall();
        }

        // Auto-detect the calling agent (unless --app already set an explicit title/preset).
        if (!explicitTitle)
        {
            var detected = DetectCallingAgent(debug);
            if (detected is not null) title = detected.Title;
        }

        if (message is null)
        {
            Console.Error.WriteLine("Error: Message is required.");
            PrintUsage();
            return 1;
        }

        var exePath = Environment.ProcessPath ?? "hookchime";
        var notifier = NotifierFactory.Create(Constants.AppId);
        var request = new NotificationRequest(title, message, iconPath);

        if (dryRun)
        {
            Console.WriteLine($"[dry-run] Title: {title}");
            Console.WriteLine($"[dry-run] Message: {message}");
            Console.WriteLine($"[dry-run] Icon: {iconPath ?? "(none)"}");
            Console.WriteLine($"[dry-run] Exe path: {exePath}");
            Console.WriteLine("[dry-run] Notifier call:");
            Console.WriteLine(notifier.DescribeDryRun(request));
            Console.WriteLine(NtfyClient.IsConfigured
                ? $"[dry-run] ntfy: would POST to {NtfyClient.DescribeTarget()}"
                : "[dry-run] ntfy: not configured");
            Console.WriteLine("[dry-run] Update check: skipped");
            return 0;
        }

        var shown = await notifier.NotifyAsync(request);
        if (!shown)
        {
            Console.Error.WriteLine("Warning: failed to show a desktop notification.");
        }

        await NtfyClient.SendAsync(title, message);

        var newerVersion = await UpdateChecker.CheckAsync(Constants.Version);
        if (newerVersion is not null)
        {
            Console.WriteLine($"Update available: v{Constants.Version} -> v{newerVersion} ({UpdateChecker.ReleasesUrl})");
        }

        return shown ? 0 : 1;
    }

    private static AppPreset? DetectCallingAgent(bool debug)
    {
        try
        {
            IProcessTreeReader? reader = null;
            if (OperatingSystem.IsWindows()) reader = new WindowsProcessTreeReader();
            else if (OperatingSystem.IsLinux()) reader = new LinuxProcessTreeReader();
            else if (OperatingSystem.IsMacOS()) reader = new MacProcessTreeReader();

            if (reader is null) return null;

            var pid = Environment.ProcessId;
            if (debug) Console.Error.WriteLine($"[DEBUG] Starting from PID: {pid}");
            return AgentDetector.Detect(reader, pid);
        }
        catch
        {
            return null; // Auto-detect is a convenience, never a hard requirement.
        }
    }

    private static int HandleInstall(string? agentFilter)
    {
        var exePath = Environment.ProcessPath ?? "hookchime";
        var targets = agentFilter is null
            ? Installers.Where(a => a.IsDetected()).ToArray()
            : Installers.Where(a => string.Equals(a.AgentName, agentFilter, StringComparison.OrdinalIgnoreCase)).ToArray();

        if (targets.Length == 0)
        {
            Console.WriteLine(agentFilter is null
                ? "No known AI CLI agents detected."
                : $"Unknown agent '{agentFilter}'. Known agents: {string.Join(", ", Installers.Select(a => a.AgentName))}");
            return 1;
        }

        var ok = true;
        foreach (var installer in targets)
        {
            var success = installer.Install(exePath);
            Console.WriteLine(success
                ? $"  [x] {installer.AgentName}: hook installed"
                : $"  [ ] {installer.AgentName}: FAILED to install hook");
            ok &= success;
        }

        return ok ? 0 : 1;
    }

    private static int HandleUninstall()
    {
        var ok = true;
        foreach (var installer in Installers)
        {
            var success = installer.Uninstall();
            Console.WriteLine(success
                ? $"  [x] {installer.AgentName}: hook removed"
                : $"  [ ] {installer.AgentName}: FAILED to remove hook");
            ok &= success;
        }

        return ok ? 0 : 1;
    }

    private static void ShowStatus()
    {
        Console.WriteLine("HookChime agent status:");
        foreach (var installer in Installers)
        {
            var detected = installer.IsDetected();
            var installed = installer.IsInstalled();
            var mark = installed ? "x" : " ";
            var detail = detected ? (installed ? "hook installed" : "detected, hook not installed") : "not detected";
            Console.WriteLine($"  [{mark}] {installer.AgentName}: {detail}");
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine($"""
            hookchime <message> [options]
            hookchime --install [agent]
            hookchime --uninstall
            hookchime --status

            Options:
              -t, --title <text>   Set notification title (default: "Notification")
              --app <name>         Use AI CLI preset ({string.Join(", ", AppPresets.All.Select(p => p.Name))})
              -i, --icon <path>    Use a custom icon
              -v, --version        Show version and exit
              -h, --help           Show this help
              --install [agent]    Install hooks for AI CLI agents ({string.Join(", ", Installers.Select(a => a.AgentName))}, or all)
              --uninstall          Remove hooks from all AI CLI agents
              --status             Show installation status
              --dry-run            Show what would happen without executing side effects
              --debug              Print agent auto-detection diagnostics to stderr
            """);
    }
}
