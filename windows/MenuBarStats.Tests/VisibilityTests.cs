using MenuBarStats.Core;
using Xunit;
namespace MenuBarStats.Tests;
public class VisibilityTests
{
    [Fact] public void MaximizedWindowDoesNotHideRevealedTaskbar() => Assert.False(VisibilityPolicy.IsFullScreen(-8,-8,1928,1088,0,0,1920,1080,true));
    [Fact] public void FullscreenWindowHidesMetrics() => Assert.True(VisibilityPolicy.IsFullScreen(0,0,1920,1080,0,0,1920,1080,false));
    [Fact] public void OrdinaryWindowDoesNotHideMetrics() => Assert.False(VisibilityPolicy.IsFullScreen(0,0,1000,700,0,0,1920,1080,false));
}
