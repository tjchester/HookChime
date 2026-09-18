using HookChime.Core.HookInstallers;

namespace HookChime.Tests;

public class CodexInstallerTests : IDisposable
{
    private readonly TempDirectory _home = new();
    private readonly CodexInstaller _installer;

    public CodexInstallerTests() => _installer = new CodexInstaller(_home.Path);

    public void Dispose() => _home.Dispose();

    private string ConfigPath => Path.Combine(_home.Path, ".codex", "config.toml");

    [Fact]
    public void Install_CreatesFileOnFreshHome()
    {
        Assert.True(_installer.Install(@"C:\tools\hookchime.exe"));
        var content = File.ReadAllText(ConfigPath);
        Assert.Contains("notify = [\"C:\\\\tools\\\\hookchime.exe\"", content);
        Assert.True(_installer.IsInstalled());
    }

    [Fact]
    public void Install_PreservesCommentsAndTablesInExistingFile()
    {
        Directory.CreateDirectory(Path.Combine(_home.Path, ".codex"));
        File.WriteAllText(ConfigPath, """
            # user's own comment about their model choice
            model = "gpt-5"

            [profile.default]
            approval = "never" # inline comment
            """);

        Assert.True(_installer.Install(@"C:\tools\hookchime.exe"));

        var content = File.ReadAllText(ConfigPath);
        Assert.Contains("# user's own comment about their model choice", content);
        Assert.Contains("model = \"gpt-5\"", content);
        Assert.Contains("[profile.default]", content);
        Assert.Contains("approval = \"never\" # inline comment", content);
        Assert.Contains("notify = [\"C:\\\\tools\\\\hookchime.exe\"", content);

        // notify must land BEFORE the first table, since TOML tables end the "top level".
        var notifyPos = content.IndexOf("notify", StringComparison.Ordinal);
        var tablePos = content.IndexOf("[profile.default]", StringComparison.Ordinal);
        Assert.True(notifyPos < tablePos);
    }

    [Fact]
    public void Install_ReplacesOwnPreviousNotifyLineRatherThanDuplicating()
    {
        Directory.CreateDirectory(Path.Combine(_home.Path, ".codex"));
        _installer.Install(@"C:\old\path\hookchime.exe");

        Assert.True(_installer.Install(@"C:\new\path\hookchime.exe"));

        var content = File.ReadAllText(ConfigPath);
        Assert.DoesNotContain("old", content);
        Assert.Contains("new", content);
        Assert.Single(content.Split('\n'), l => l.TrimStart().StartsWith("notify"));
    }

    [Fact]
    public void Install_DoesNotTouchAnUnrelatedNotifyEntry()
    {
        // Codex only supports one top-level `notify` key, but if a user already
        // pointed it at their own tool, HookChime's install should still take over
        // that key (matching the single-notify-key reality of the format) rather
        // than silently no-op-ing — this asserts it actually replaces it, and does
        // so exactly once.
        Directory.CreateDirectory(Path.Combine(_home.Path, ".codex"));
        File.WriteAllText(ConfigPath, "notify = [\"/usr/bin/my-notifier\", \"done\"]\n");

        Assert.True(_installer.Install(@"C:\tools\hookchime.exe"));

        var content = File.ReadAllText(ConfigPath);
        Assert.Contains("hookchime.exe", content);
        Assert.Single(content.Split('\n'), l => l.TrimStart().StartsWith("notify"));
    }

    [Fact]
    public void Uninstall_RemovesNotifyLineButKeepsRestOfFile()
    {
        Directory.CreateDirectory(Path.Combine(_home.Path, ".codex"));
        File.WriteAllText(ConfigPath, "model = \"gpt-5\"\n");
        _installer.Install(@"C:\tools\hookchime.exe");

        Assert.True(_installer.Uninstall());

        var content = File.ReadAllText(ConfigPath);
        Assert.Contains("model = \"gpt-5\"", content);
        Assert.DoesNotContain("hookchime", content);
    }
}
