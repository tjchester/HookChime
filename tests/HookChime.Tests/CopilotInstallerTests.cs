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
}
