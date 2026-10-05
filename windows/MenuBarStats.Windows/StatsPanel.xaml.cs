using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using MenuBarStats.Core;
namespace MenuBarStats.Windows;
public partial class StatsPanel : Window
{
    private readonly StatsViewModel model;
    internal StatsPanel(StatsViewModel model) { InitializeComponent(); this.model = model; DataContext = model; StartWithWindows.IsChecked = ((App)Application.Current).Settings.StartWithWindows; }
    internal void Position(TaskbarState state, NativeMethods.Rect label)
    {
        // Native coordinates avoid WPF's mixed-DPI desktop coordinate ambiguities.
        MaxHeight = Placement.ToLogical(state.Monitor.Height, state.Dpi) - 16; UpdateLayout();
        var handle = new WindowInteropHelper(this).EnsureHandle();
        var scale = state.Dpi / 96; var width = (int)Math.Round(Width * scale); var height = (int)Math.Round(ActualHeight * scale);
        if (height <= 0) height = (int)Math.Round(DesiredSize.Height * scale);
        var x = label.Left; var y = label.Top - height - 8;
        if (state.Horizontal && state.Bar.Top < state.Monitor.Top + state.Monitor.Height / 2) y = label.Bottom + 8;
        if (!state.Horizontal) { x = state.Bar.Left < state.Monitor.Left + state.Monitor.Width / 2 ? label.Right + 8 : label.Left - width - 8; y = label.Top; }
        x = (int)Placement.Clamp(x, width, state.Monitor.Left + 8, state.Monitor.Right - 8);
        y = (int)Placement.Clamp(y, height, state.Monitor.Top + 8, state.Monitor.Bottom - 8);
        NativeMethods.SetWindowPos(handle, NativeMethods.Topmost, x, y, 0, 0, NativeMethods.NoSize | NativeMethods.NoActivate);
    }
    private void PanelKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) { Close(); e.Handled = true; } }
    private void PanelDeactivated(object sender, EventArgs e) => Close();
    private async void RefreshClicked(object sender, RoutedEventArgs e) => await model.RefreshManuallyAsync();
    private void ExitClicked(object sender, RoutedEventArgs e) => ((App)Application.Current).ExitApp();
    private void StartupClicked(object sender, RoutedEventArgs e)
    {
        var app = (App)Application.Current;
        try { app.StartupIntegration.SetEnabled(StartWithWindows.IsChecked == true); app.Settings.StartWithWindows = StartWithWindows.IsChecked == true; app.SaveSettings(); SettingsError.Visibility = Visibility.Collapsed; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or COMException or System.Security.SecurityException)
        {
            app.Settings.StartWithWindows = app.StartupIntegration.IsEnabled; StartWithWindows.IsChecked = app.Settings.StartWithWindows; SettingsError.Visibility = Visibility.Visible;
        }
    }
}
