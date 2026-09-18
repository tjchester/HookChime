using System.Text.Json;

namespace HookChime.Core.UpdateCheck;

/// <summary>
/// Checks GitHub Releases for a newer version, throttled to once per 24h via
/// <see cref="StateStore"/>. Non-blocking (5s timeout) and silent on any failure —
/// an update check must never be the reason a notification fails to show.
/// </summary>
public static class UpdateChecker
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"{Constants.AppName}-CLI");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    /// <summary>Returns the newer remote version tag if one is available and it's time to check, else null.</summary>
    public static async Task<string?> CheckAsync(string currentVersion, CancellationToken ct = default)
    {
        var state = StateStore.Load();
        if (state.LastUpdateCheckUtc is { } last && DateTimeOffset.UtcNow - last < TimeSpan.FromHours(24))
        {
            return null;
        }

        state.LastUpdateCheckUtc = DateTimeOffset.UtcNow;
        StateStore.Save(state);

        try
        {
            var url = $"https://api.github.com/repos/{Constants.RepoOwner}/{Constants.RepoName}/releases/latest";
            using var response = await Http.GetAsync(url, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("tag_name", out var tagProp)) return null;

            var remoteTag = tagProp.GetString();
            if (string.IsNullOrEmpty(remoteTag)) return null;

            var remoteVersion = remoteTag.TrimStart('v', 'V');
            return VersionComparer.IsNewer(currentVersion, remoteVersion) ? remoteVersion : null;
        }
        catch
        {
            return null;
        }
    }

    public static string ReleasesUrl => $"https://github.com/{Constants.RepoOwner}/{Constants.RepoName}/releases";
}
