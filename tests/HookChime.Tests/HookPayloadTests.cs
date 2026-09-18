using HookChime.Core;

namespace HookChime.Tests;

public class HookPayloadTests
{
    [Fact]
    public void ParseJson_ClaudeCodeStopHookShape()
    {
        var json = """
            {
              "session_id": "abc123",
              "transcript_path": "/home/user/.claude/projects/foo/abc123.jsonl",
              "cwd": "/home/user/repos/HookChime",
              "hook_event_name": "Stop",
              "stop_hook_active": false
            }
            """;

        var payload = HookPayloadReader.ParseJson(json);

        Assert.NotNull(payload);
        Assert.Equal("abc123", payload!.SessionId);
        Assert.Equal("Stop", payload.HookEventName);
        Assert.Equal("HookChime", payload.ProjectName);
    }

    [Fact]
    public void ParseJson_CopilotSessionEndShape()
    {
        var json = """{"timestamp": 1704618000000, "cwd": "C:\\Users\\tjche\\Desktop\\GitHub\\HookChime", "reason": "complete"}""";

        var payload = HookPayloadReader.ParseJson(json);

        Assert.NotNull(payload);
        Assert.Equal("complete", payload!.Reason);
        Assert.Equal("HookChime", payload.ProjectName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("[1,2,3]")]
    [InlineData("\"just a string\"")]
    public void ParseJson_ReturnsNullForUnusablePayloads(string? text)
    {
        Assert.Null(HookPayloadReader.ParseJson(text));
    }

    [Fact]
    public void ProjectName_NullWhenCwdMissing()
    {
        var payload = HookPayloadReader.ParseJson("""{"session_id": "abc123"}""");

        Assert.NotNull(payload);
        Assert.Null(payload!.ProjectName);
    }

    [Fact]
    public void ProjectName_HandlesTrailingSlash()
    {
        var payload = HookPayloadReader.ParseJson("""{"cwd": "/home/user/repos/HookChime/"}""");

        Assert.Equal("HookChime", payload!.ProjectName);
    }
}
