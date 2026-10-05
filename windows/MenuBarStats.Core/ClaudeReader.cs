using System.Net.Http.Headers;
using System.Text.Json;
namespace MenuBarStats.Core;
public sealed class ClaudeReader(HttpClient client)
{
    private readonly SemaphoreSlim gate=new(1,1);
    public async Task<UsageLimits?> FetchAsync(string configRoot,CancellationToken cancellationToken)
    {
        try {
            var json=await File.ReadAllTextAsync(Path.Combine(configRoot,".credentials.json"),cancellationToken);
            using var document=JsonDocument.Parse(json);
            var root=document.RootElement;
            if(root.ValueKind!=JsonValueKind.Object || !root.TryGetProperty("claudeAiOauth",out var oauth) || oauth.ValueKind!=JsonValueKind.Object || !oauth.TryGetProperty("accessToken",out var token) || token.ValueKind!=JsonValueKind.String || string.IsNullOrWhiteSpace(token.GetString())) return null;
            return await FetchTokenAsync(token.GetString()!,cancellationToken);
        } catch(IOException) { return null; } catch(UnauthorizedAccessException) { return null; } catch(JsonException) { return null; }
    }
    public async Task<UsageLimits?> FetchTokenAsync(string token,CancellationToken cancellationToken)
    {
        if(!await gate.WaitAsync(0,cancellationToken)) return null;
        try {
            using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            using var request=new HttpRequestMessage(HttpMethod.Get,"https://api.anthropic.com/api/oauth/usage");
            request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
            request.Headers.Add("anthropic-beta","oauth-2025-04-20");
            using var response=await client.SendAsync(request,timeout.Token);
            if(!response.IsSuccessStatusCode) return null;
            return LimitsParser.ParseClaude(await response.Content.ReadAsStringAsync(timeout.Token));
        } catch(HttpRequestException) { return null; }
          catch(OperationCanceledException) when(!cancellationToken.IsCancellationRequested) { return null; }
          catch(FormatException) { return null; }
        finally { gate.Release(); }
    }
}
