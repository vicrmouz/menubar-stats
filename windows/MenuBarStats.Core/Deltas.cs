namespace MenuBarStats.Core;
public static class Deltas
{
    public static double Cpu(ulong idleDelta,ulong kernelDelta,ulong userDelta)
    {
        var total=(double)kernelDelta+userDelta;
        return total<=0 ? 0 : Math.Clamp((total-idleDelta)/total,0,1);
    }
    public static double Rate(long previous,long current,double elapsedSeconds) => elapsedSeconds<=0 || !double.IsFinite(elapsedSeconds) || current<previous ? 0 : Math.Max(0,((double)current-previous)/elapsedSeconds);
}
