using System.Text.Json.Nodes;

namespace HookChime.Core.HookInstallers;

/// <summary>
/// Shared logic for the three JSON-based agent hook formats (Claude, Gemini, Copilot).
/// Each stores an array of hook entries; entries may carry a "command" directly or
/// nest it under an inner "hooks" array (Claude/Gemini's format).
/// </summary>
internal static class JsonHookUtils
{
    public static bool ContainsHookChimeEntry(JsonArray array)
    {
        foreach (var item in array)
        {
            if (item is not JsonObject obj) continue;
            if (CommandMentionsHookChime(obj)) return true;

            if (obj["hooks"] is JsonArray innerHooks)
            {
                foreach (var inner in innerHooks)
                {
                    if (inner is JsonObject innerObj && CommandMentionsHookChime(innerObj))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public static JsonArray RemoveHookChimeEntries(JsonArray array)
    {
        var result = new JsonArray();
        foreach (var item in array)
        {
            var clone = item?.DeepClone();
            if (clone is JsonObject obj)
            {
                if (CommandMentionsHookChime(obj)) continue;

                if (obj["hooks"] is JsonArray innerHooks)
                {
                    var keptInner = new JsonArray();
                    foreach (var inner in innerHooks)
                    {
                        if (inner is JsonObject innerObj && CommandMentionsHookChime(innerObj)) continue;
                        keptInner.Add(inner?.DeepClone());
                    }
                    if (keptInner.Count == 0) continue;
                    obj["hooks"] = keptInner;
                }
            }
            result.Add(clone);
        }
        return result;
    }

    private static bool CommandMentionsHookChime(JsonObject obj)
    {
        foreach (var key in new[] { "command", "bash", "powershell" })
        {
            if (obj[key] is JsonValue value &&
                value.TryGetValue(out string? cmd) &&
                cmd.Contains(Constants.ExeMarker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
