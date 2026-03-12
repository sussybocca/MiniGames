namespace MiniGames.Services;

public class TrippyService
{
    private readonly Random _rand = new();
    public bool IsTrippyMode { get; set; } = true;

    public string GetTrippyClass()
    {
        if (!IsTrippyMode) return "";
        string[] classes = {
            "trippy-spin", "trippy-rainbow", "trippy-wave",
            "trippy-pulse", "trippy-shake", "trippy-glitch"
        };
        return classes[_rand.Next(classes.Length)];
    }

    public (string effect, int duration) GetRandomEffect()
    {
        var effects = new[]
        {
            ("spin", 2000),
            ("rainbow", 3000),
            ("wave", 1500),
            ("pulse", 1000),
            ("shake", 500),
            ("glitch", 800)
        };
        return effects[_rand.Next(effects.Length)];
    }
}