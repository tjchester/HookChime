using System.Text.Json;
using HookChime.Core.HookInstallers;

namespace HookChime.Tests;

public class ClaudeInstallerTests : IDisposable
{
    private readonly TempDirectory _home = new();
    private readonly ClaudeInstaller _installer;

    public ClaudeInstallerTests() => _installer = new ClaudeInstaller(_home.Path);

    public void Dispose() => _home.Dispose();

    [Fact]
    public void IsDetected_FalseWhenNoClaudeDir()
    {
        Assert.False(_installer.IsDetected());
    }

    [Fact]
    public void Install_CreatesConfigWithHookOnFreshHome()
    {
        Directory.CreateDirectory(Path.Combine(_home.Path, ".claude"));

        Assert.True(_installer.IsDetected());
        Assert.False(_installer.IsInstalled());

        Assert.True(_installer.Install(@"C:\tools\hookchime.exe"));

        Assert.True(_installer.IsInstalled());
        var content = File.ReadAllText(Path.Combine(_home.Path, ".claude", "settings.json"));
        Assert.Contains("hookchime.exe", content);
        Assert.Contains("\"Stop\"", content);
    }

    [Fact]
    public void Install_PreservesExistingUnrelatedHooksAndSettings()
    {
        var claudeDir = Path.Combine(_home.Path, ".claude");
        Directory.CreateDirectory(claudeDir);
        File.WriteAllText(Path.Combine(claudeDir, "settings.json"), """
            {
              "theme": "dark",
              "hooks": {
                "Stop": [
                  { "hooks": [ { "type": "command", "command": "someOtherTool.exe ping", "timeout": 3000 } ] }
                ]
              }
            }
            """);

        Assert.True(_installer.Install(@"C:\tools\hookchime.exe"));

        var content = File.ReadAllText(Path.Combine(claudeDir, "settings.json"));
        Assert.Contains("someOtherTool.exe", content);
        Assert.Contains("hookchime.exe", content);
        Assert.Contains("\"dark\"", content);
    }

    [Fact]
    public void Install_IsIdempotent()
    {
        Directory.CreateDirectory(Path.Combine(_home.Path, ".claude"));
        Assert.True(_installer.Install(@"C:\tools\hookchime.exe"));
        var afterFirst = File.ReadAllText(Path.Combine(_home.Path, ".claude", "settings.json"));

        Assert.True(_installer.Install(@"C:\tools\hookchime.exe"));
        var afterSecond = File.ReadAllText(Path.Combine(_home.Path, ".claude", "settings.json"));

        Assert.Equal(afterFirst, afterSecond);
    }

    [Fact]
    public void Uninstall_RemovesOnlyHookChimeEntry()
    {
        var claudeDir = Path.Combine(_home.Path, ".claude");
        Directory.CreateDirectory(claudeDir);
        File.WriteAllText(Path.Combine(claudeDir, "settings.json"), """
            {
              "hooks": {
                "Stop": [
                  { "hooks": [ { "type": "command", "command": "someOtherTool.exe ping", "timeout": 3000 } ] }
                ]
              }
            }
            """);
        _installer.Install(@"C:\tools\hookchime.exe");

        Assert.True(_installer.Uninstall());

        var content = File.ReadAllText(Path.Combine(claudeDir, "settings.json"));
        Assert.Contains("someOtherTool.exe", content);
        Assert.DoesNotContain("hookchime.exe", content);
        Assert.False(_installer.IsInstalled());
    }

    [Fact]
    public void Uninstall_NoOpWhenNothingInstalled()
    {
        Assert.True(_installer.Uninstall());
    }

    [Fact]
    public void Install_QuotesWindowsPathSafelyForBashExecution()
    {
        // Regression test: Claude Code runs the "command" string through bash (even on
        // Windows, e.g. via Git Bash). Unquoted, bash treats \ as an escape character
        // and strips it before the next char, mangling ANY Windows path — this one has
        // no spaces, which used to be (wrongly) treated as "safe, no quoting needed".
        Directory.CreateDirectory(Path.Combine(_home.Path, ".claude"));
        _installer.Install(@"C:\Users\tjche\.local\bin\hookchime.exe");

        var json = File.ReadAllText(Path.Combine(_home.Path, ".claude", "settings.json"));
        using var doc = JsonDocument.Parse(json);
        var command = doc.RootElement
            .GetProperty("hooks").GetProperty("Stop")[0]
            .GetProperty("hooks")[0].GetProperty("command").GetString();

        // The decoded command value must have each backslash doubled, so that after
        // bash's own double-quote unescaping, the exe path survives with single
        // backslashes intact.
        var expected = """
            "C:\\Users\\tjche\\.local\\bin\\hookchime.exe" "Task complete" -t "Claude Code"
            """;
        Assert.Equal(expected, command);
    }
}
