namespace SlayTheSpire2Ultrawide.Tests;

public class AspectMathTests
{
    [Theory]
    [InlineData(1920, 1080, 1.000)]
    [InlineData(2560, 1080, 1.333)]
    [InlineData(3440, 1440, 1.344)]
    [InlineData(3840, 1600, 1.350)]
    [InlineData(5120, 1440, 2.000)]
    [InlineData(5120, 2160, 1.333)]
    public void CameraXMultiplier_MatchesLadder(int w, int h, double expected)
    {
        var m = AspectMath.CameraXMultiplier(w, h);
        Assert.Equal(expected, m, precision: 3);
    }

    [Fact]
    public void IsUltrawide_TrueWhenAspectExceeds16x9PlusEpsilon()
    {
        Assert.False(AspectMath.IsUltrawide(1920, 1080));
        Assert.False(AspectMath.IsUltrawide(1366, 768));
        Assert.True(AspectMath.IsUltrawide(2560, 1080));
        Assert.True(AspectMath.IsUltrawide(5120, 1440));
    }

    [Fact]
    public void ResolutionLadder_ReturnsAllSupportedResolutions()
    {
        var ladder = AspectMath.ResolutionLadder().ToList();
        Assert.Contains((2560, 1080), ladder);
        Assert.Contains((3440, 1440), ladder);
        Assert.Contains((3840, 1600), ladder);
        Assert.Contains((5120, 1440), ladder);
        Assert.Contains((5120, 2160), ladder);
    }
}
