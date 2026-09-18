using System.Text.Json;
using HookChime.Core.HookInstallers;

namespace HookChime.Tests;

public class CopilotInstallerTests : IDisposable
{
    private readonly TempDirectory _repo = new();
    private readonly CopilotInstaller _installer;

    public CopilotInstallerTests() => _installer = new CopilotInstaller(_repo.Path);

    public void Dispose() => _repo.Dispose();

    [Fact]
    public void IsDetected_TrueWhenGithubDirExists()
    {
        Directory.CreateDirectory(Path.Combine(_repo.Path, ".github"));
        Assert.True(_installer.IsDetected());
    }

    [Fact]
    public void Install_CreatesDedicatedHookChimeJsonFile()
    {
        Directory.CreateDirectory(Path.Combine(_repo.Path, ".github"));

        Assert.True(_installer.Install(@"C:\tools\hookchime.exe"));

        var path = Path.Combine(_repo.Path, ".github", "hooks", "hookchime.json");
        Assert.True(File.Exists(path));
        var content = File.ReadAllText(path);
        Assert.Contains("sessionEnd", content);
        Assert.Contains("hookchime", content);
        Assert.True(_installer.IsInstalled());
    }

    [Fact]
    public void Uninstall_DeletesTheDedicatedFile()
    {
        Directory.CreateDirectory(Path.Combine(_repo.Path, ".github"));
        _installer.Install(@"C:\tools\hookchime.exe");

        Assert.True(_installer.Uninstall());

        Assert.False(File.Exists(Path.Combine(_repo.Path, ".github", "hooks", "hookchime.json")));
    }

    [Fact]
    public void Install_PowerShellCommandKeepsSingleBackslashes()
    {
        // PowerShell's escape char is backtick, not backslash, so (unlike the bash-run
        // Claude/Gemini commands) this path must NOT have its backslashes doubled.
        Directory.CreateDirectory(Path.Combine(_repo.Path, ".github"));
        _installer.Install(@"C:\Users\tjche\.local\bin\hookchime.exe");

        var json = File.ReadAllText(Path.Combine(_repo.Path, ".github", "hooks", "hookchime.json"));
        using var doc = JsonDocument.Parse(json);
        var powershell = doc.RootElement
            .GetProperty("hooks").GetProperty("sessionEnd")[0]
            .GetProperty("powershell").GetString();

        Assert.Equal(
            "\"C:\\Users\\tjche\\.local\\bin\\hookchime.exe\" 'Copilot finished' -t 'GitHub Copilot'",
            powershell);
    }
}
