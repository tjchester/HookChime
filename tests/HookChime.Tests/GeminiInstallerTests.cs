using HookChime.Core.HookInstallers;

namespace HookChime.Tests;

public class GeminiInstallerTests : IDisposable
{
    private readonly TempDirectory _home = new();
    private readonly GeminiInstaller _installer;

    public GeminiInstallerTests() => _installer = new GeminiInstaller(_home.Path);

    public void Dispose() => _home.Dispose();

    [Fact]
    public void Install_EnablesHooksAndAddsAfterAgentEntry()
    {
        Directory.CreateDirectory(Path.Combine(_home.Path, ".gemini"));

        Assert.True(_installer.Install(@"C:\tools\hookchime.exe"));

        var content = File.ReadAllText(Path.Combine(_home.Path, ".gemini", "settings.json"));
        Assert.Contains("\"enabled\": true", content);
        Assert.Contains("\"AfterAgent\"", content);
        Assert.Contains("hookchime.exe", content);
        Assert.True(_installer.IsInstalled());
    }

    [Fact]
    public void Uninstall_RemovesOnlyHookChimeEntry()
    {
        var dir = Path.Combine(_home.Path, ".gemini");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "settings.json"), """
            {
              "hooks": {
                "enabled": true,
                "AfterAgent": [
                  { "matcher": "*", "hooks": [ { "type": "command", "command": "someOtherTool" } ] }
                ]
              }
            }
            """);
        _installer.Install(@"C:\tools\hookchime.exe");

        Assert.True(_installer.Uninstall());

        var content = File.ReadAllText(Path.Combine(dir, "settings.json"));
        Assert.Contains("someOtherTool", content);
        Assert.DoesNotContain("hookchime.exe", content);
    }
}
