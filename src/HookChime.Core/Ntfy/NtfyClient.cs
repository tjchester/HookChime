using System.Text;

namespace HookChime.Core.Ntfy;

/// <summary>
/// Optional push notification via ntfy.sh (or a self-hosted ntfy server), configured
/// entirely through environment variables: HOOKCHIME_NTFY_TOPIC / HOOKCHIME_NTFY_SERVER.
/// Fire-and-forget: any failure here must never affect the local desktop notification.
/// </summary>
public static class NtfyClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };

    public static bool IsConfigured =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HOOKCHIME_NTFY_TOPIC"));

    public static string DescribeTarget()
    {
        var topic = Environment.GetEnvironmentVariable("HOOKCHIME_NTFY_TOPIC") ?? "";
        return $"https://{ResolveServer()}/{topic}";
    }

    private static string ResolveServer()
    {
        var server = Environment.GetEnvironmentVariable("HOOKCHIME_NTFY_SERVER");
        return string.IsNullOrEmpty(server) ? "ntfy.sh" : server;
    }

    public static async Task SendAsync(string title, string message, CancellationToken ct = default)
    {
        var topic = Environment.GetEnvironmentVariable("HOOKCHIME_NTFY_TOPIC");
        if (string.IsNullOrEmpty(topic)) return;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{ResolveServer()}/{topic}")
            {
                Content = new StringContent(message, Encoding.UTF8)
            };
            request.Headers.TryAddWithoutValidation("Title", title);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            using var response = await Http.SendAsync(request, cts.Token).ConfigureAwait(false);
        }
        catch
        {
            // Offline, misconfigured topic, server down, etc. — silently skip.
        }
    }
}
