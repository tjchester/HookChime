namespace HookChime.Core.HookInstallers;

/// <summary>One coding agent's hook wiring: detect it, check/install/uninstall its notification hook.</summary>
public interface IAgentInstaller
{
    string AgentName { get; }

    /// <summary>True if this agent appears to be present on the machine (e.g. its config dir exists).</summary>
    bool IsDetected();

    /// <summary>True if a HookChime hook is already wired into this agent's config.</summary>
    bool IsInstalled();

    /// <summary>Writes (or updates) the hook pointing at <paramref name="exePath"/>. Idempotent.</summary>
    bool Install(string exePath);

    /// <summary>Removes any HookChime hook from this agent's config, leaving everything else untouched.</summary>
    bool Uninstall();
}
