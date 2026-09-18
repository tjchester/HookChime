using System.Text.Json;

namespace HookChime.Core;

public sealed class StateData
{
    public DateTimeOffset? LastUpdateCheckUtc { get; set; }
}

/// <summary>
/// Persists small cross-run state (currently just the update-check throttle) to
/// ~/.hookchime/state.json. Uses the user-profile folder directly rather than
/// each OS's "special" app-data folder so the on-disk location is one thing to
/// document, not three.
/// </summary>
public static class StateStore
{
    private static string DirPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".hookchime");

    private static string FilePath => Path.Combine(DirPath, "state.json");

    public static StateData Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new StateData();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<StateData>(json) ?? new StateData();
        }
        catch
        {
            return new StateData();
        }
    }

    public static void Save(StateData data)
    {
        try
        {
            Directory.CreateDirectory(DirPath);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Best-effort: failing to persist state must never break a notification.
        }
    }
}
