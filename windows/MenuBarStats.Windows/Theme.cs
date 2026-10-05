using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
namespace MenuBarStats.Windows;
internal static class Theme
{
    private static bool? lastDark;
    private static bool lastContrast;
    public static void Update()
    {
        var dark = false;
        try { using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"); dark = key?.GetValue("SystemUsesLightTheme") is int value && value == 0; } catch (System.Security.SecurityException) { }
        var contrast = SystemParameters.HighContrast;
        if (lastDark == dark && lastContrast == contrast) return; lastDark = dark; lastContrast = contrast;
        var resources = Application.Current.Resources;
        void Set(string key, string light, string night) => resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? night : light));
        Set("Surface", "#F5F5F5", "#292929"); Set("Foreground", "#202020", "#F5F5F5"); Set("Secondary", "#595959", "#BEBEBE"); Set("Rule", "#D7D7D7", "#494949"); Set("Track", "#DEDEDE", "#494949"); Set("Accent", "#006FE6", "#61A8FF");
        if (contrast) { resources["Surface"] = SystemColors.WindowBrush; resources["Foreground"] = SystemColors.WindowTextBrush; resources["Secondary"] = SystemColors.WindowTextBrush; resources["Accent"] = SystemColors.HighlightBrush; resources["Track"] = SystemColors.ControlBrush; resources["Rule"] = SystemColors.WindowTextBrush; }
    }
}
