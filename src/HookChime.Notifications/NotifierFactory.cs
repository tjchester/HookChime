using System.Runtime.InteropServices;
using HookChime.Notifications.Linux;
using HookChime.Notifications.MacOS;
#if WINDOWS
using HookChime.Notifications.Windows;
#endif

namespace HookChime.Notifications;

public static class NotifierFactory
{
    public static INotifier Create(string appId)
    {
#if WINDOWS
        if (OperatingSystem.IsWindows()) return new WindowsToastNotifier(appId);
#endif
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return new LinuxNotifier();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return new MacNotifier();

        throw new PlatformNotSupportedException(
            $"HookChime has no notifier for {RuntimeInformation.OSDescription}.");
    }
}
