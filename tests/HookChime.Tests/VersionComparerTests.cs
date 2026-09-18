using HookChime.Core;

namespace HookChime.Tests;

public class VersionComparerTests
{
    [Theory]
    [InlineData("0.1.0", "0.2.0", true)]
    [InlineData("0.2.0", "0.1.0", false)]
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("1.0", "1.0.1", true)]
    [InlineData("1.9.0", "1.10.0", true)]
    [InlineData("2.0.0", "1.99.99", false)]
    public void IsNewer_ComparesDottedVersionsNumerically(string current, string remote, bool expected)
    {
        Assert.Equal(expected, VersionComparer.IsNewer(current, remote));
    }
}
