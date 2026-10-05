using MenuBarStats.Core;
using Xunit;
namespace MenuBarStats.Tests;
public class LimitsTests
{
    [Fact] public void ExpiredCodexWindowIsZero()
    {
        var json = """{"payload":{"rate_limits":{"primary":{"used_percent":80,"resets_at":1},"secondary":{"used_percent":20}}}}""";
        var result = LimitsParser.ParseCodex(json, DateTimeOffset.FromUnixTimeSeconds(2));
        Assert.NotNull(result); Assert.Equal(0, result.Session.Percent); Assert.Equal(20, result.Weekly.Percent);
    }
    [Theory]
    [InlineData("{")][InlineData("{}")] [InlineData("null")]
    [InlineData("{\"five_hour\":{\"utilization\":\"bad\"}}")]
    public void MalformedDataIsUnavailable(string json) { Assert.Null(LimitsParser.ParseClaude(json)); Assert.Null(LimitsParser.ParseCodex(json, DateTimeOffset.UtcNow)); }
    [Fact] public void ClaudeKeepsPercentWhenResetIsInvalid()
    {
        var result = LimitsParser.ParseClaude("""{"five_hour":{"utilization":70,"resets_at":"bad"},"seven_day":{"utilization":200}}""");
        Assert.NotNull(result); Assert.Equal(70, result.Session.Percent); Assert.Null(result.Session.ResetsAt); Assert.Equal(100, result.Weekly.Percent);
    }
    [Fact] public void CodexReaderFindsNewestEventAndIgnoresPartialLine()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()); Directory.CreateDirectory(root);
        try {
            File.WriteAllText(Path.Combine(root,"new.jsonl"), """{"timestamp":"2026-10-05T10:00:00Z","payload":{"rate_limits":{"primary":{"used_percent":40},"secondary":{"used_percent":60}}}}""" + "\n{\"payload\":");
            File.WriteAllText(Path.Combine(root,"old.jsonl"), """{"timestamp":"2026-10-04T10:00:00Z","payload":{"rate_limits":{"primary":{"used_percent":10},"secondary":{"used_percent":20}}}}""");
            var result = CodexReader.Read(root, DateTimeOffset.Parse("2026-10-05T12:00:00Z"));
            Assert.NotNull(result); Assert.Equal(40, result.Session.Percent);
        } finally { Directory.Delete(root,true); }
    }
    [Fact] public void MissingSessionsDirectoryIsUnavailable() => Assert.Null(CodexReader.Read(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString()), DateTimeOffset.UtcNow));
}
