using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using MenuBarStats.Core;
namespace MenuBarStats.Windows;
public partial class App : Application
{
    private Mutex? instance;
    private bool ownsMutex;
    private bool stopping;
    internal StatsViewModel Model { get; private set; } = null!;
    internal Preferences Settings { get; private set; } = null!;
    internal string SettingsPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MenuBarStats", "settings.json");
    internal StartupService StartupIntegration { get; private set; } = null!;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var sid = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        instance = new Mutex(true, @"Local\MenuBarStats-" + sid, out ownsMutex);
        if (!ownsMutex) { Shutdown(); return; }
        Settings = Preferences.Load(SettingsPath);
        StartupIntegration = new StartupService(Environment.GetFolderPath(Environment.SpecialFolder.Startup), Environment.ProcessPath!, CreateShortcut, OwnsShortcut);
        Settings.StartWithWindows = StartupIntegration.IsEnabled;
        Theme.Update(); Model = new StatsViewModel(Dispatcher);
        var window = new TaskbarWindow(Model); MainWindow = window; window.Show(); Model.Start();
    }
    internal void SaveSettings() => Settings.Save(SettingsPath);
    internal async void ExitApp()
    {
        if (stopping) return; stopping = true;
        if (MainWindow is TaskbarWindow taskbar) taskbar.Stop();
        try { await Model.StopAsync(); } finally { Shutdown(); }
    }
    private static void CreateShortcut(string path, string target)
    {
        var type = Type.GetTypeFromProgID("WScript.Shell") ?? throw new IOException("Windows Script Host indisponível.");
        object? shell = null, link = null;
        try
        {
            shell = Activator.CreateInstance(type)!;
            dynamic automation = shell; link = automation.CreateShortcut(path); dynamic shortcut = link;
            shortcut.TargetPath = target; shortcut.WorkingDirectory = Path.GetDirectoryName(target); shortcut.Description = StartupService.OwnershipMarker; shortcut.Save();
        }
        finally { if (link is not null) Marshal.FinalReleaseComObject(link); if (shell is not null) Marshal.FinalReleaseComObject(shell); }
    }
    private static bool OwnsShortcut(string path)
    {
        object? shell = null, link = null;
        try
        {
            var type = Type.GetTypeFromProgID("WScript.Shell"); if (type is null) return false;
            shell = Activator.CreateInstance(type)!; dynamic automation = shell;
            link = automation.CreateShortcut(path); dynamic shortcut = link;
            return string.Equals((string?)shortcut.Description, StartupService.OwnershipMarker, StringComparison.Ordinal);
        }
        catch (COMException) { return false; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
        finally { if (link is not null) Marshal.FinalReleaseComObject(link); if (shell is not null) Marshal.FinalReleaseComObject(shell); }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        Model?.Dispose(); if (ownsMutex) instance?.ReleaseMutex(); instance?.Dispose(); base.OnExit(e);
    }
}
