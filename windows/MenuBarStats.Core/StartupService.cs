namespace MenuBarStats.Core;
public sealed class StartupService(string directory,string executable,Action<string,string> createShortcut,Func<string,bool> ownsShortcut)
{
    public const string OwnershipMarker="MenuBarStats managed startup shortcut {A5C8BB35-1863-4EB4-8989-4A80FBD1C2CD}";
    private string Shortcut=>Path.Combine(directory,"MenuBarStats.lnk");
    public bool IsEnabled=>File.Exists(Shortcut) && ownsShortcut(Shortcut);
    public void SetEnabled(bool enabled)
    {
        if(File.Exists(Shortcut) && !ownsShortcut(Shortcut)) {
            if(enabled) throw new IOException("Já existe um atalho de outro aplicativo com esse nome.");
            return;
        }
        if(enabled) {Directory.CreateDirectory(directory); createShortcut(Shortcut,executable);}
        else if(File.Exists(Shortcut)) File.Delete(Shortcut);
    }
}
