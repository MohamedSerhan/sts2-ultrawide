namespace SlayTheSpire2Ultrawide;

public static class AspectMath
{
    public const double BaseAspect = 16.0 / 9.0;

    public static double CameraXMultiplier(int width, int height)
    {
        var aspect = (double)width / height;
        return Math.Round(aspect / BaseAspect, 3);
    }

    public static bool IsUltrawide(int width, int height)
    {
        const double epsilon = 0.01;
        return (double)width / height > BaseAspect + epsilon;
    }

    public static IEnumerable<(int Width, int Height)> ResolutionLadder() =>
        new[]
        {
            (2560, 1080),
            (3440, 1440),
            (3840, 1600),
            (5120, 1440),
            (5120, 2160),
        };
}
