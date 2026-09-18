namespace HookChime.Core.ProcessTree;

public sealed record ProcessInfo(int Pid, string Name, string? CommandLine = null);

/// <summary>Walks the parent-process chain so we can tell which coding agent invoked us.</summary>
public interface IProcessTreeReader
{
    /// <summary>Yields ancestors starting from the immediate parent of <paramref name="startPid"/>, walking up to the root.</summary>
    IEnumerable<ProcessInfo> WalkAncestors(int startPid);
}
