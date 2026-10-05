using MenuBarStats.Core;
using Xunit;
namespace MenuBarStats.Tests;
public class GeometryTests
{
    [Fact] public void CounterResetDoesNotProduceNegativeRate()
    { Assert.Equal(0,Deltas.Rate(1000,20,2)); Assert.Equal(500,Deltas.Rate(1000,2000,2)); Assert.Equal(0.75,Deltas.Cpu(25,80,20)); }
    [Theory][InlineData(0)][InlineData(-2)] public void InvalidSampleDurationIsZero(double time) => Assert.Equal(0,Deltas.Rate(1,100,time));
    [Fact] public void IdleCannotMakeCpuNegative() => Assert.Equal(0,Deltas.Cpu(300,80,20));
    [Fact] public void EmptyCpuSampleIsZero() => Assert.Equal(0,Deltas.Cpu(0,0,0));
    [Theory][InlineData(-100,300,-1920,0,-300)][InlineData(-2500,300,-1920,0,-1920)][InlineData(10,500,0,300,0)]
    public void PlacementStaysInsideMonitor(double origin,double size,double min,double max,double expected) => Assert.Equal(expected,Placement.Clamp(origin,size,min,max));
    [Theory][InlineData(150,144,100)][InlineData(200,192,100)]
    public void DpiCoordinatesUseLogicalUnits(double physical,double dpi,double logical) => Assert.Equal(logical,Placement.ToLogical(physical,dpi));
    [Fact] public void PreferencesCorruptionDoesNotLoseDefaults()
    {
        var file=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".json");
        try {File.WriteAllText(file,"{"); var prefs=Preferences.Load(file); Assert.False(prefs.StartWithWindows); Assert.Null(prefs.Offset);}
        finally {File.Delete(file);}
    }
    [Fact] public void PreferencesRoundTripWithoutCredentials()
    {
        var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString()); var file=Path.Combine(dir,"settings.json");
        try {new Preferences {Offset=140,StartWithWindows=true}.Save(file); var prefs=Preferences.Load(file); Assert.Equal(140,prefs.Offset); Assert.True(prefs.StartWithWindows);}
        finally {Directory.Delete(dir,true);}
    }
    [Fact] public void NonFinitePreferencesAreRejected()
    {
        var file=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".json");
        try {File.WriteAllText(file,"{\"Offset\":-10,\"StartWithWindows\":false}"); Assert.Null(Preferences.Load(file).Offset);}
        finally {File.Delete(file);}
    }
}
