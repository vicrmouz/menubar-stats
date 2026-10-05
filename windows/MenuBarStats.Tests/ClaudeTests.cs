using System.Net;
using MenuBarStats.Core;
using Xunit;
namespace MenuBarStats.Tests;
public class ClaudeTests
{
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> run) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token) => run(request,token); }
    [Theory][InlineData(HttpStatusCode.Unauthorized)][InlineData(HttpStatusCode.TooManyRequests)]
    public async Task HttpErrorsAreUnavailable(HttpStatusCode status)
    {
        using var client = new HttpClient(new Handler((_,_)=>Task.FromResult(new HttpResponseMessage(status))));
        var reader = new ClaudeReader(client);
        Assert.Null(await reader.FetchTokenAsync("synthetic",CancellationToken.None));
    }
    [Fact] public async Task RequestUsesOnlyAnthropicEndpointAndOAuthHeaders()
    {
        using var client = new HttpClient(new Handler((request,_)=> {
            Assert.Equal("https://api.anthropic.com/api/oauth/usage",request.RequestUri!.ToString());
            Assert.Equal("Bearer synthetic",request.Headers.Authorization!.ToString());
            Assert.Equal("oauth-2025-04-20",request.Headers.GetValues("anthropic-beta").Single());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("""{"five_hour":{"utilization":30},"seven_day":{"utilization":50}}""")});
        }));
        var result = await new ClaudeReader(client).FetchTokenAsync("synthetic",CancellationToken.None);
        Assert.NotNull(result); Assert.Equal(30,result.Session.Percent);
    }
    [Fact] public async Task CancellationStopsBlockedRequest()
    {
        using var client = new HttpClient(new Handler(async (_,token)=> { await Task.Delay(Timeout.Infinite,token); return new HttpResponseMessage(); }));
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>new ClaudeReader(client).FetchTokenAsync("synthetic",cancel.Token));
    }
    [Fact] public async Task MissingCredentialsAreUnavailable()
    {
        using var client=new HttpClient();
        Assert.Null(await new ClaudeReader(client).FetchAsync(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString()),CancellationToken.None));
    }
    [Fact] public async Task OverlappingRefreshDoesNotStartAnotherRequest()
    {
        var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls=0;
        using var client=new HttpClient(new Handler(async (_,token)=>{Interlocked.Increment(ref calls); entered.TrySetResult(); await release.Task.WaitAsync(token); return new HttpResponseMessage(HttpStatusCode.Unauthorized);}));
        var reader=new ClaudeReader(client);
        var first=reader.FetchTokenAsync("synthetic",CancellationToken.None); await entered.Task;
        Assert.Null(await reader.FetchTokenAsync("synthetic",CancellationToken.None));
        release.SetResult(); await first; Assert.Equal(1,calls);
    }
}
