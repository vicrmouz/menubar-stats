using System.Globalization;
using System.Text.Json;
namespace MenuBarStats.Core;
public static class LimitsParser
{
    public static UsageLimits? ParseClaude(string json) => Parse(json, null);
    public static UsageLimits? ParseCodex(string json, DateTimeOffset now) => Parse(json, now);
    private static UsageLimits? Parse(string json, DateTimeOffset? now)
    {
        try {
            using var document=JsonDocument.Parse(json);
            var root=document.RootElement;
            if(root.ValueKind!=JsonValueKind.Object) return null;
            DateTimeOffset? recorded=null;
            if(now.HasValue) {
                if(root.TryGetProperty("timestamp",out var stamp) && stamp.ValueKind==JsonValueKind.String && DateTimeOffset.TryParse(stamp.GetString(),CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var date)) recorded=date;
                if(!root.TryGetProperty("payload",out root) || root.ValueKind!=JsonValueKind.Object || !root.TryGetProperty("rate_limits",out root) || root.ValueKind!=JsonValueKind.Object) return null;
            }
            var session=Window(root,now.HasValue?"primary":"five_hour",now);
            var weekly=Window(root,now.HasValue?"secondary":"seven_day",now);
            return session is null || weekly is null ? null : new(session,weekly,recorded);
        } catch(JsonException) { return null; }
    }
    private static LimitWindow? Window(JsonElement root,string key,DateTimeOffset? now)
    {
        if(!root.TryGetProperty(key,out var window) || window.ValueKind!=JsonValueKind.Object || !window.TryGetProperty(now.HasValue?"used_percent":"utilization",out var value) || value.ValueKind!=JsonValueKind.Number || !value.TryGetDouble(out var percent) || !double.IsFinite(percent)) return null;
        DateTimeOffset? reset=null;
        if(window.TryGetProperty("resets_at",out var raw)) {
            if(now.HasValue && raw.ValueKind==JsonValueKind.Number && raw.TryGetInt64(out var epoch)) {
                try { reset=DateTimeOffset.FromUnixTimeSeconds(epoch); } catch(ArgumentOutOfRangeException) { }
            } else if(raw.ValueKind==JsonValueKind.String && DateTimeOffset.TryParse(raw.GetString(),CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var date)) reset=date;
        }
        if(now.HasValue && reset<=now) return new(0,null);
        return new(Math.Clamp(percent,0,100),reset);
    }
}
