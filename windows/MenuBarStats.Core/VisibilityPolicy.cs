namespace MenuBarStats.Core;
public static class VisibilityPolicy
{
    public static bool IsFullScreen(int left,int top,int right,int bottom,int monitorLeft,int monitorTop,int monitorRight,int monitorBottom,bool maximized)
        => !maximized && left<=monitorLeft && top<=monitorTop && right>=monitorRight && bottom>=monitorBottom;
}
