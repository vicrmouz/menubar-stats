using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
namespace MenuBarStats.Windows;
public partial class TaskbarWindow : Window
{
    private readonly StatsViewModel model;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private StatsPanel? panel;
    private TaskbarState? state;
    private NativeMethods.Rect label;
    private NativeMethods.Point start;
    private bool pressed, dragging;
    private double? initialOffset;
    private nint handle;
    internal TaskbarWindow(StatsViewModel model)
    {
        InitializeComponent(); this.model = model; DataContext = model;
        SourceInitialized += (_, _) => { handle = new WindowInteropHelper(this).Handle; UpdatePosition(); timer.Start(); };
        timer.Tick += (_, _) => UpdatePosition();
        Closing += (_, e) => { if (timer.IsEnabled) { e.Cancel = true; ((App)Application.Current).ExitApp(); } };
    }
    private void UpdatePosition()
    {
        Theme.Update(); state = TaskbarHost.Read(handle);
        if (state is null || !state.Visible) { if (IsVisible) Hide(); panel?.Close(); return; }
        if (pressed) return;
        label = TaskbarHost.LabelRect(state, ((App)Application.Current).Settings.Offset);
        if (!IsVisible) Show(); TaskbarHost.Place(handle, label);
        if (panel is { IsVisible: true }) panel.Position(state, label);
    }
    private void OpenPanel()
    {
        if (panel is not null) { panel.Close(); return; }
        if (state is null || !state.Visible) return;
        panel = new StatsPanel(model) { Owner = this };
        panel.Closed += (_, _) => panel = null;
        panel.Show(); panel.Position(state, label); panel.Activate();
    }
    private void LabelKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space) { OpenPanel(); e.Handled = true; }
        if (e.Key == Key.Escape) { panel?.Close(); e.Handled = true; }
    }
    private void DragStarted(object sender, MouseButtonEventArgs e)
    {
        NativeMethods.GetCursorPos(out start); pressed = true; dragging = false; initialOffset = ((App)Application.Current).Settings.Offset;
        CaptureMouse(); e.Handled = true;
    }
    private void DragMoved(object sender, MouseEventArgs e)
    {
        if (!pressed || state is null || e.LeftButton != MouseButtonState.Pressed) return;
        NativeMethods.GetCursorPos(out var cursor); var delta = state.Horizontal ? cursor.X - start.X : cursor.Y - start.Y;
        if (Math.Abs(delta) < 4 && !dragging) return; dragging = true; panel?.Close();
        var basis = initialOffset ?? (state.Horizontal ? label.Left - state.Bar.Left : label.Top - state.Bar.Top) * 96 / state.Dpi;
        var offset = Math.Max(0, basis + delta * 96 / state.Dpi);
        ((App)Application.Current).Settings.Offset = offset;
        TaskbarHost.Place(handle, TaskbarHost.LabelRect(state, offset));
    }
    private void DragEnded(object sender, MouseButtonEventArgs e)
    {
        if (!pressed) return; var wasDragged = dragging; pressed = false; ReleaseMouseCapture();
        if (wasDragged)
        {
            if (state is not null) { label = TaskbarHost.LabelRect(state, ((App)Application.Current).Settings.Offset); ((App)Application.Current).Settings.Offset = (state.Horizontal ? label.Left - state.Bar.Left : label.Top - state.Bar.Top) * 96 / state.Dpi; }
            try { ((App)Application.Current).SaveSettings(); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        else OpenPanel(); dragging = false; e.Handled = true;
    }
    private void DragCancelled(object sender, MouseEventArgs e) { pressed = false; }
    internal void Stop() { timer.Stop(); panel?.Close(); }
}
