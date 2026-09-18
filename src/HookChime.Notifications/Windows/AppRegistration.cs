using System.Runtime.Versioning;

namespace HookChime.Notifications.Windows;

/// <summary>
/// Windows toast notifications from an unpackaged console app require a Start Menu
/// shortcut carrying the app's AppUserModelID — otherwise CreateToastNotifier throws
/// "no toast notifier was found". This creates that shortcut once, silently, on first run.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class AppRegistration
{
    private static string ShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Programs), "HookChime.lnk");

    public static bool IsRegistered() => File.Exists(ShortcutPath);

    public static bool EnsureRegistered(string appId)
    {
        if (IsRegistered()) return true;

        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) return false;

        return ShellLinkInterop.CreateShortcutWithAppId(
            exePath, appId, "HookChime - cross-platform notification CLI", ShortcutPath);
    }
}
