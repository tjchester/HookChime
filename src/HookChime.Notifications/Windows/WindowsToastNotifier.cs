using System.Runtime.Versioning;
using System.Security;
using System.Text;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace HookChime.Notifications.Windows;

/// <summary>
/// Shows a native Windows toast via the same WinRT ToastNotificationManager API the
/// original C++ tool used, projected automatically by targeting a windows10.0 TFM
/// (no CommunityToolkit/WinAppSDK dependency needed for this).
/// </summary>
[SupportedOSPlatform("windows10.0.10240.0")]
public sealed class WindowsToastNotifier : INotifier
{
    private readonly string _appId;

    public WindowsToastNotifier(string appId) => _appId = appId;

    public Task<bool> NotifyAsync(NotificationRequest request, CancellationToken ct = default)
    {
        try
        {
            AppRegistration.EnsureRegistered(_appId);

            var xml = BuildToastXml(request);
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var toast = new ToastNotification(doc);
            var notifier = ToastNotificationManager.CreateToastNotifier(_appId);
            notifier.Show(toast);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public string DescribeDryRun(NotificationRequest request) => BuildToastXml(request);

    private static string BuildToastXml(NotificationRequest request)
    {
        var sb = new StringBuilder();
        sb.Append("<toast><visual><binding template=\"ToastGeneric\">");
        if (!string.IsNullOrEmpty(request.IconPath))
        {
            sb.Append("<image placement=\"appLogoOverride\" src=\"").Append(Escape(request.IconPath)).Append("\"/>");
        }
        sb.Append("<text>").Append(Escape(request.Title)).Append("</text>");
        sb.Append("<text>").Append(Escape(request.Message)).Append("</text>");
        sb.Append("</binding></visual></toast>");
        return sb.ToString();
    }

    private static string Escape(string value) => SecurityElement.Escape(value) ?? value;
}
