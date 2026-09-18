namespace HookChime.Tests;

/// <summary>A throwaway directory for a single test, deleted on dispose. Never touches real user config.</summary>
public sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hookchime-tests-" + Guid.NewGuid());

    public TempDirectory() => Directory.CreateDirectory(Path);

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch { /* best-effort cleanup */ }
    }
}
