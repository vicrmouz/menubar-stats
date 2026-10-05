using System.Text;
namespace MenuBarStats.Core;
public static class CodexReader
{
    public static UsageLimits? Read(string sessionsRoot,DateTimeOffset now)
    {
        try {
            // Limit each read to the tail and the most recently written files.
            var files=Directory.EnumerateFiles(sessionsRoot,"*.jsonl",SearchOption.AllDirectories)
                .Select(path=>new FileInfo(path)).OrderByDescending(f=>f.LastWriteTimeUtc).Take(20).ToArray();
            UsageLimits? latest=null;
            DateTimeOffset latestDate=DateTimeOffset.MinValue;
            foreach(var file in files) {
                try {
                    using var stream=new FileStream(file.FullName,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
                    var offset=Math.Max(0,stream.Length-512_000); stream.Seek(offset,SeekOrigin.Begin);
                    using var reader=new StreamReader(stream,Encoding.UTF8,detectEncodingFromByteOrderMarks:true);
                    if(offset>0) reader.ReadLine(); // discard potentially cut first JSON line
                    var lines=reader.ReadToEnd().Split('\n');
                    foreach(var line in lines.Reverse()) {
                        if(!line.Contains("\"rate_limits\"",StringComparison.Ordinal)) continue;
                        var limits=LimitsParser.ParseCodex(line,now); if(limits is null) continue;
                        var date=limits.RecordedAt ?? new DateTimeOffset(file.LastWriteTimeUtc,TimeSpan.Zero);
                        if(date>latestDate) { latest=limits with {RecordedAt=date}; latestDate=date; }
                    }
                } catch(IOException) { } catch(UnauthorizedAccessException) { }
            }
            return latest;
        } catch(IOException) { return null; } catch(UnauthorizedAccessException) { return null; }
    }
}
