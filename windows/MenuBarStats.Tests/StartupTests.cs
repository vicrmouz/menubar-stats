using MenuBarStats.Core;
using Xunit;
namespace MenuBarStats.Tests;
public class StartupTests
{
    [Fact] public void ConflictingShortcutIsPreservedWhenDisabling()
    {
        var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString()); Directory.CreateDirectory(dir);
        var link=Path.Combine(dir,"MenuBarStats.lnk");
        try {File.WriteAllText(link,"someone else"); new StartupService(dir,"app.exe",(_,_)=>{},_=>false).SetEnabled(false); Assert.True(File.Exists(link)); Assert.Equal("someone else",File.ReadAllText(link));}
        finally {Directory.Delete(dir,true);}
    }

    [Fact] public void DisablingStartupLeavesOtherShortcutsUntouched()
    {
        var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString()); Directory.CreateDirectory(dir);
        try {
            File.WriteAllText(Path.Combine(dir,"Other.lnk"),"keep"); File.WriteAllText(Path.Combine(dir,"MenuBarStats.lnk"),"own");
            new StartupService(dir,"app.exe",(_,_)=>throw new InvalidOperationException(),path=>File.ReadAllText(path)=="own").SetEnabled(false);
            Assert.True(File.Exists(Path.Combine(dir,"Other.lnk"))); Assert.False(File.Exists(Path.Combine(dir,"MenuBarStats.lnk")));
        } finally {Directory.Delete(dir,true);}
    }
    [Fact] public void EnablingCreatesOnlyAppShortcut()
    {
        var dir=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());
        try {
            new StartupService(dir,"C:/MenuBarStats.exe",(path,target)=>File.WriteAllText(path,target),_=>true).SetEnabled(true);
            Assert.Equal("C:/MenuBarStats.exe",File.ReadAllText(Path.Combine(dir,"MenuBarStats.lnk")));
        } finally {Directory.Delete(dir,true);}
    }
}
