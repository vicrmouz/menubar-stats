namespace MenuBarStats.Core;
public static class Placement
{
    public static double Clamp(double origin,double size,double minimum,double maximum) => Math.Clamp(origin,minimum,Math.Max(minimum,maximum-size));
    public static double ToLogical(double physical,double dpi) => physical*96/(dpi>0 ? dpi : 96);
}
