using System;
using System.Collections.Generic;
using System.Linq;
using Timer = System.Timers.Timer;

namespace MiniGames.Services;

public class RouteChaosService : IDisposable
{
    private readonly Random _rand = new();
    private readonly Dictionary<string, string> _fakeToReal = new();
    private readonly Dictionary<string, List<string>> _realToFakes = new();
    private Timer? _chaosTimer;
    private int _timeLeft; // seconds
    private List<string>? _allRealPaths;

    public bool IsChaosActive { get; private set; }
    public int TimeLeft => _timeLeft;

    // Call this once at startup with all real paths (e.g., from GameService + "/", "/about")
    public void InitializeRealPaths(IEnumerable<string> realPaths)
    {
        _allRealPaths = realPaths.ToList();
    }

    public void StartChaos()
    {
        if (IsChaosActive || _allRealPaths == null || _allRealPaths.Count == 0)
            return;

        // Generate 200 fake URLs and map them to the real paths
        GenerateFakeMappings(_allRealPaths, 200);

        IsChaosActive = true;
        _timeLeft = 40;

        _chaosTimer = new Timer(1000);
        _chaosTimer.Elapsed += (s, e) =>
        {
            _timeLeft--;
            if (_timeLeft <= 0)
            {
                StopChaos();
            }
        };
        _chaosTimer.Start();
    }

    public void StopChaos()
    {
        _chaosTimer?.Stop();
        _chaosTimer?.Dispose();
        _chaosTimer = null;
        IsChaosActive = false;
        _fakeToReal.Clear();
        _realToFakes.Clear();
        _timeLeft = 0;
    }

    // Given a real path, return a fake version if chaos is active; otherwise return the path unchanged.
    public string GetFakePath(string realPath)
    {
        if (!IsChaosActive) return realPath;

        if (_realToFakes.TryGetValue(realPath, out var fakes) && fakes.Count > 0)
        {
            return fakes[_rand.Next(fakes.Count)];
        }
        return realPath; // fallback
    }

    // Given a fake path, return the real path if chaos is active and mapping exists; otherwise null.
    public string? GetRealPath(string fakePath)
    {
        if (!IsChaosActive) return fakePath; // not active → treat as real path

        _fakeToReal.TryGetValue(fakePath, out var real);
        return real;
    }

    private void GenerateFakeMappings(List<string> realPaths, int totalFakes)
    {
        _fakeToReal.Clear();
        _realToFakes.Clear();

        foreach (var real in realPaths)
        {
            _realToFakes[real] = new List<string>();
        }

        var memeWords = new[]
        {
            "dank", "meme", "404", "error", "blue-screen", "crash", "glitch", "rekt",
            "hack", "root", "kernel", "panic", "overload", "lag", "spaghetti",
            "taco", "cat", "doge", "nyan", "rickroll", "password", "admin",
            "secret", "hidden", "void", "null", "undefined", "NaN", "infinity",
            "hackerman", "1337", "fail", "win", "troll", "facepalm", "lol", "omg",
            "wtf", "bbq", "derp", "yolo", "swag", "kappa", "pogchamp", "feelsbadman",
            "feelsgoodman", "wow", "such", "very", "much", "amaze", "so", "plz",
            "halp", "help", "me", "you", "them", "we", "us", "they",
            "gib", "gibberish", "foobar", "baz", "qux", "xyzzy", "plugh", "asdf",
            "qwerty", "zxcv", "uiop", "jkl", "bnm", "lorem", "ipsum", "dolor"
        };

        for (int i = 0; i < totalFakes; i++)
        {
            string real = realPaths[_rand.Next(realPaths.Count)];
            string fake;
            do
            {
                string word = memeWords[_rand.Next(memeWords.Length)];
                string number = _rand.Next(1000, 9999).ToString();
                fake = $"/{word}/{word}-{number}";
            } while (_fakeToReal.ContainsKey(fake));

            _fakeToReal[fake] = real;
            _realToFakes[real].Add(fake);
        }
    }

    public void Dispose()
    {
        _chaosTimer?.Dispose();
    }
}