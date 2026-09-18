namespace HookChime.Notifications;

public sealed record NotificationRequest(string Title, string Message, string? IconPath = null);
