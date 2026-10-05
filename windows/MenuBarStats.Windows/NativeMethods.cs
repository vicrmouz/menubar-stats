using System.Runtime.InteropServices;
namespace MenuBarStats.Windows;
internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left, Top, Right, Bottom; public int Width => Right - Left; public int Height => Bottom - Top; }
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct MemoryStatus { public uint Length, Load; public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile, TotalVirtual, AvailableVirtual, AvailableExtendedVirtual; }
    [StructLayout(LayoutKind.Sequential)] internal struct MonitorInfo { public uint Size; public Rect Monitor, Work; public uint Flags; }
    [StructLayout(LayoutKind.Sequential)] internal struct WindowPlacement { public uint Length, Flags, ShowCmd; public Point MinPosition, MaxPosition; public Rect NormalPosition; }
    [DllImport("kernel32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetSystemTimes(out ulong idle, out ulong kernel, out ulong user);
    [DllImport("kernel32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern nint FindWindow(string? className, string? title);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern nint FindWindowEx(nint parent, nint after, string? className, string? title);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetWindowRect(nint window, out Rect rect);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetWindowPlacement(nint window, ref WindowPlacement placement);
    [DllImport("user32.dll")] internal static extern nint MonitorFromWindow(nint window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(nint window);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetClassName(nint window, System.Text.StringBuilder text, int count);
    [DllImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool IsZoomed(nint window);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] internal static extern nint GetWindowLongPtr(nint window, int index);
    internal static readonly nint Topmost = new(-1);
    internal const uint NoActivate = 0x0010, NoSize = 0x0001, NoMove = 0x0002, ShowWindow = 0x0040;
}
