using System.Runtime.InteropServices;
using System.Text;
using MenuBarStats.Core;
namespace MenuBarStats.Windows;
internal sealed record TaskbarState(NativeMethods.Rect Bar, NativeMethods.Rect Monitor, NativeMethods.Rect Work, double Dpi, bool Horizontal, bool Visible, int ClockStart);
internal static class TaskbarHost
{
    public static TaskbarState? Read(nint ownWindow)
    {
        var bar = NativeMethods.FindWindow("Shell_TrayWnd", null);
        if (bar == 0 || !NativeMethods.GetWindowRect(bar, out var rect)) return null;
        var monitor = new NativeMethods.MonitorInfo { Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (!NativeMethods.GetMonitorInfo(NativeMethods.MonitorFromWindow(bar, 2), ref monitor)) return null;
        var horizontal = rect.Width > rect.Height;
        var intersectionWidth = Math.Max(0, Math.Min(rect.Right, monitor.Monitor.Right) - Math.Max(rect.Left, monitor.Monitor.Left));
        var intersectionHeight = Math.Max(0, Math.Min(rect.Bottom, monitor.Monitor.Bottom) - Math.Max(rect.Top, monitor.Monitor.Top));
        var visible = NativeMethods.IsWindowVisible(bar) && (horizontal ? intersectionHeight >= Math.Min(8, rect.Height) : intersectionWidth >= Math.Min(8, rect.Width));
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground != 0 && foreground != ownWindow && foreground != bar)
        {
            var name = new StringBuilder(128); NativeMethods.GetClassName(foreground, name, name.Capacity);
            if (name.ToString() is not ("Progman" or "WorkerW" or "Shell_SecondaryTrayWnd") && NativeMethods.GetWindowRect(foreground, out var full))
            {
                // A maximized desktop window may cover monitor bounds with auto-hide;
                // distinguish it from a borderless fullscreen application.
                var captioned = (NativeMethods.GetWindowLongPtr(foreground, -16).ToInt64() & 0x00C00000L) != 0;
                var maximizedDesktop = NativeMethods.IsZoomed(foreground) && captioned;
                visible &= !VisibilityPolicy.IsFullScreen(full.Left, full.Top, full.Right, full.Bottom, monitor.Monitor.Left, monitor.Monitor.Top, monitor.Monitor.Right, monitor.Monitor.Bottom, maximizedDesktop);
            }
        }
        var tray = NativeMethods.FindWindowEx(bar, 0, "TrayNotifyWnd", null);
        var clockStart = horizontal ? rect.Right - 200 : rect.Bottom - 200;
        if (tray != 0 && NativeMethods.GetWindowRect(tray, out var trayRect)) clockStart = horizontal ? trayRect.Left : trayRect.Top;
        var dpi = NativeMethods.GetDpiForWindow(bar);
        return new(rect, monitor.Monitor, monitor.Work, dpi == 0 ? 96 : dpi, horizontal, visible, clockStart);
    }
    public static NativeMethods.Rect LabelRect(TaskbarState state, double? offset)
    {
        var scale = state.Dpi / 96; var width = (int)Math.Round(148 * scale); var height = (int)Math.Round(28 * scale);
        var bar = state.Bar;
        if (state.Horizontal)
        {
            height = Math.Min(height, bar.Height);
            var left = (int)Placement.Clamp(offset is { } o ? bar.Left + o * scale : state.ClockStart - width - 8 * scale, width, bar.Left, bar.Right);
            var top = bar.Top + (bar.Height - height) / 2;
            return new() { Left = left, Top = top, Right = left + width, Bottom = top + height };
        }
        width = Math.Min(width, bar.Width);
        var y = (int)Placement.Clamp(offset is { } v ? bar.Top + v * scale : state.ClockStart - height - 8 * scale, height, bar.Top, bar.Bottom);
        var x = bar.Left + (bar.Width - width) / 2;
        return new() { Left = x, Top = y, Right = x + width, Bottom = y + height };
    }
    public static void Place(nint handle, NativeMethods.Rect rect) => NativeMethods.SetWindowPos(handle, NativeMethods.Topmost, rect.Left, rect.Top, rect.Width, rect.Height, NativeMethods.NoActivate);
}
