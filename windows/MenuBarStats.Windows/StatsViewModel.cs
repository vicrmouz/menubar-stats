using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using MenuBarStats.Core;
namespace MenuBarStats.Windows;
internal sealed class StatsViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly Dispatcher dispatcher;
    private readonly SystemReader system = new();
    private readonly HttpClient client = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan };
    private readonly ClaudeReader claudeReader;
    private readonly CancellationTokenSource lifetime = new();
    private readonly string claudeRoot = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");
    private readonly string sessionsRoot = Path.Combine(Environment.GetEnvironmentVariable("CODEX_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex"), "sessions");
    private int refreshing;
    private Task[] loops = [];
    public StatsViewModel(Dispatcher dispatcher) { this.dispatcher = dispatcher; claudeReader = new(client); }
    public event PropertyChangedEventHandler? PropertyChanged;
    public SystemSnapshot? Snapshot { get; private set; }
    public UsageLimits? Claude { get; private set; }
    public UsageLimits? Codex { get; private set; }
    public bool IsRefreshing { get; private set; }
    public bool CanRefresh => !IsRefreshing;
    public string CpuLabel => Snapshot?.Cpu is { } cpu ? $"{cpu * 100:0}%" : "—";
    public string MemoryLabel => Snapshot?.MemoryTotal is > 0 && Snapshot.MemoryUsed is { } used ? $"{100.0 * used / Snapshot.MemoryTotal:0}%" : "—";
    public double CpuValue => (Snapshot?.Cpu ?? 0) * 100;
    public double MemoryValue => Snapshot?.MemoryTotal is > 0 && Snapshot.MemoryUsed is { } used ? 100.0 * used / Snapshot.MemoryTotal.Value : 0;
    public double DiskValue => Snapshot?.DiskTotal is > 0 && Snapshot.DiskFree is { } free ? 100.0 * (Snapshot.DiskTotal.Value - free) / Snapshot.DiskTotal.Value : 0;
    public string MemoryDetail => Snapshot?.MemoryTotal is { } total && Snapshot.MemoryUsed is { } used ? $"{Gb(used)} de {Gb(total)}" : "Sem dados";
    public string DiskDetail => Snapshot?.DiskFree is { } free ? $"{Gb(free)} livres" : "Sem dados";
    public string NetworkDetail => Snapshot is { } s ? $"↓ {Rate(s.DownloadRate)}   ↑ {Rate(s.UploadRate)}" : "Sem dados";
    public string CodexAge => Codex?.RecordedAt is { } date ? $"Última atividade: {Relative(date)}" : "";
    public void Start() => loops = [Loop(TimeSpan.FromSeconds(2), ReadSystem), Loop(TimeSpan.FromMinutes(1), ReadCodex), Loop(TimeSpan.FromMinutes(5), RefreshClaudeAsync)];
    private async Task Loop(TimeSpan interval, Func<CancellationToken, Task> action)
    {
        using var timer = new PeriodicTimer(interval);
        try { do { await action(lifetime.Token); } while (await timer.WaitForNextTickAsync(lifetime.Token)); }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
    }
    private async Task ReadSystem(CancellationToken token)
    {
        var snapshot = await Task.Run(system.Read, token);
        await dispatcher.InvokeAsync(() => { Snapshot = snapshot; NotifyAll(); });
    }
    private async Task ReadCodex(CancellationToken token)
    {
        var result = await Task.Run(() => CodexReader.Read(sessionsRoot, DateTimeOffset.UtcNow), token);
        await dispatcher.InvokeAsync(() => { Codex = result; NotifyAll(); });
    }
    public async Task RefreshClaudeAsync(CancellationToken token)
    {
        if (Interlocked.Exchange(ref refreshing, 1) != 0) return;
        try
        {
            await dispatcher.InvokeAsync(() => { IsRefreshing = true; NotifyAll(); });
            var result = await claudeReader.FetchAsync(claudeRoot, token);
            await dispatcher.InvokeAsync(() => { Claude = result; NotifyAll(); });
        }
        finally { Interlocked.Exchange(ref refreshing, 0); if (!dispatcher.HasShutdownStarted) await dispatcher.InvokeAsync(() => { IsRefreshing = false; NotifyAll(); }); }
    }
    public async Task RefreshManuallyAsync() { try { await RefreshClaudeAsync(lifetime.Token); } catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { } }
    private void NotifyAll() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    internal static string Gb(double bytes) => (bytes / 1_000_000_000).ToString("0.0", CultureInfo.GetCultureInfo("pt-BR")) + " GB";
    internal static string Rate(double value) => value >= 1_000_000 ? $"{value / 1_000_000:0.0} MB/s" : value >= 1_000 ? $"{value / 1_000:0} KB/s" : $"{value:0} B/s";
    internal static string Relative(DateTimeOffset date)
    {
        var time = date - DateTimeOffset.Now; var ahead = time.TotalSeconds >= 0; var duration = time.Duration();
        var value = duration.TotalDays >= 1 ? $"{(int)duration.TotalDays} d" : duration.TotalHours >= 1 ? $"{(int)duration.TotalHours} h" : $"{Math.Max(1, (int)duration.TotalMinutes)} min";
        return ahead ? $"em {value}" : $"há {value}";
    }
    public async Task StopAsync() { lifetime.Cancel(); await Task.WhenAll(loops); }
    public void Dispose() { lifetime.Cancel(); client.Dispose(); lifetime.Dispose(); }
}
