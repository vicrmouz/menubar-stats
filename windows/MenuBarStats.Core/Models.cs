namespace MenuBarStats.Core;
public sealed record LimitWindow(double Percent, DateTimeOffset? ResetsAt);
public sealed record UsageLimits(LimitWindow Session, LimitWindow Weekly, DateTimeOffset? RecordedAt = null);
public sealed record SystemSnapshot(double? Cpu, ulong? MemoryUsed, ulong? MemoryTotal, long? DiskFree, long? DiskTotal, double DownloadRate, double UploadRate);
