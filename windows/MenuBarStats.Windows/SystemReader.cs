using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using MenuBarStats.Core;
namespace MenuBarStats.Windows;
internal sealed class SystemReader
{
    private (ulong Idle, ulong Kernel, ulong User)? previousCpu;
    private Dictionary<string, (long Down, long Up)> previousNetwork = new();
    private long previousTime;
    public SystemSnapshot Read()
    {
        double? cpu = null;
        if (NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user))
        {
            cpu = previousCpu is { } p && idle >= p.Idle && kernel >= p.Kernel && user >= p.User ? Deltas.Cpu(idle - p.Idle, kernel - p.Kernel, user - p.User) : 0;
            previousCpu = (idle, kernel, user);
        }
        var memory = new NativeMethods.MemoryStatus { Length = (uint)Marshal.SizeOf<NativeMethods.MemoryStatus>() };
        ulong? used = null, total = null;
        if (NativeMethods.GlobalMemoryStatusEx(ref memory)) { total = memory.TotalPhysical; used = memory.TotalPhysical - memory.AvailablePhysical; }
        long? free = null, diskTotal = null;
        try { var disk = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!); free = disk.AvailableFreeSpace; diskTotal = disk.TotalSize; } catch (IOException) { } catch (UnauthorizedAccessException) { }
        var now = Stopwatch.GetTimestamp(); var elapsed = previousTime == 0 ? 0 : (now - previousTime) / (double)Stopwatch.Frequency;
        var current = new Dictionary<string, (long Down, long Up)>(); double down = 0, up = 0;
        try
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            {
                try
                {
                    var stats = adapter.GetIPStatistics(); current[adapter.Id] = (stats.BytesReceived, stats.BytesSent);
                    if (previousNetwork.TryGetValue(adapter.Id, out var p)) { down += Deltas.Rate(p.Down, stats.BytesReceived, elapsed); up += Deltas.Rate(p.Up, stats.BytesSent, elapsed); }
                }
                catch (NetworkInformationException) { }
            }
        }
        catch (NetworkInformationException) { }
        previousNetwork = current; previousTime = now;
        return new(cpu, used, total, free, diskTotal, down, up);
    }
}
