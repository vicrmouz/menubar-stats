using System.Text.Json;
namespace MenuBarStats.Core;
public sealed class Preferences
{
    public double? Offset {get;set;}
    public bool StartWithWindows {get;set;}
    public static Preferences Load(string path)
    {
        try {
            var preferences=JsonSerializer.Deserialize<Preferences>(File.ReadAllText(path)) ?? new();
            if(preferences.Offset is { } offset && (!double.IsFinite(offset)||offset<0)) preferences.Offset=null;
            return preferences;
        } catch(IOException) {return new();} catch(UnauthorizedAccessException) {return new();} catch(JsonException) {return new();}
    }
    public void Save(string path)
    {
        var directory=Path.GetDirectoryName(Path.GetFullPath(path))!; Directory.CreateDirectory(directory);
        var temp=Path.Combine(directory,Guid.NewGuid()+".tmp");
        try {File.WriteAllText(temp,JsonSerializer.Serialize(this)); File.Move(temp,path,true);}
        finally {if(File.Exists(temp)) File.Delete(temp);}
    }
}
