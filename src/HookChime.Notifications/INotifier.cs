namespace HookChime.Notifications;

public interface INotifier
{
    /// <summary>Shows the notification. Returns false (never throws) if the OS notification mechanism failed.</summary>
    Task<bool> NotifyAsync(NotificationRequest request, CancellationToken ct = default);

    /// <summary>Describes what NotifyAsync would do, without doing it — used by --dry-run.</summary>
    string DescribeDryRun(NotificationRequest request);
}
