using System.Text.Json;

namespace HookChime.Core;

/// <summary>
/// The JSON payload coding agents pipe over stdin to their hook commands — e.g. Claude
/// Code's Stop hook sends {"session_id", "transcript_path", "cwd", "hook_event_name",
/// "stop_hook_active"}, GitHub Copilot's sessionEnd hook sends {"timestamp", "cwd",
/// "reason"}. Field names are looked up defensively (snake_case and camelCase) since
/// this isn't tied to one agent's exact schema, and any parse failure is silently
/// treated as "no payload" — enrichment is a nice-to-have, never a reason to fail.
/// </summary>
public sealed record HookPayload(
    string? Cwd,
    string? SessionId,
    string? HookEventName,
    string? TranscriptPath,
    string? Reason)
{
    /// <summary>
    /// The last path segment of Cwd (e.g. a repo/folder name), or null if Cwd is unavailable.
    /// Splits on both '/' and '\' itself: Path.GetFileName only honors '\' on Windows, so a
    /// Windows-style cwd reported by an agent would come back whole on Linux/macOS.
    /// </summary>
    public string? ProjectName
    {
        get
        {
            if (string.IsNullOrEmpty(Cwd)) return null;

            var trimmed = Cwd.TrimEnd('/', '\\');
            var name = trimmed[(trimmed.LastIndexOfAny(['/', '\\']) + 1)..];

            // A bare drive root ("C:\") leaves just "C:", which isn't a project name.
            return name.Length > 0 && !name.EndsWith(':') ? name : null;
        }
    }
}

public static class HookPayloadReader
{
    /// <summary>Reads and parses stdin only if it's actually redirected (piped), never blocking on an interactive terminal.</summary>
    public static HookPayload? TryReadFromStdin()
    {
        if (!Console.IsInputRedirected) return null;

        try
        {
            var text = Console.In.ReadToEnd();
            return ParseJson(text);
        }
        catch
        {
            return null;
        }
    }

    public static HookPayload? ParseJson(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            return new HookPayload(
                Cwd: GetString(root, "cwd"),
                SessionId: GetString(root, "session_id") ?? GetString(root, "sessionId"),
                HookEventName: GetString(root, "hook_event_name") ?? GetString(root, "hookEventName"),
                TranscriptPath: GetString(root, "transcript_path") ?? GetString(root, "transcriptPath"),
                Reason: GetString(root, "reason"));
        }
        catch
        {
            return null;
        }
    }

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
