using Microsoft.JSInterop;
using System.Collections.Generic;

namespace MiniGames.Services;

public class ChaosService
{
    private readonly Random _rand = new();
    private DateTime _lastConsoleError = DateTime.MinValue;
    private readonly TimeSpan _consoleErrorCooldown = TimeSpan.FromMilliseconds(500);
    private int _chaosLevel = 1; // 0=off, 1=normal, 2=insane (not used yet, but can be extended)

    public bool IsChaosModeEnabled { get; set; } = true;

    // Expanded theme classes with weighting
    private readonly (string className, int weight)[] _themeClasses = new[]
    {
        ("chaos-glitch", 30),
        ("chaos-invert", 15),
        ("chaos-rotate", 10),
        ("chaos-blur", 10),
        ("chaos-shake", 8),
        ("chaos-pixelate", 5),
        ("chaos-wave", 5),
        ("chaos-vhs", 5),
        ("chaos-static", 4),
        ("chaos-color-shift", 4),
        ("chaos-lag", 3),
        ("chaos-corrupt", 1)
    };

    // Expanded error messages grouped by type
    private readonly Dictionary<string, string[]> _errorGroups = new()
    {
        ["js"] = new[]
        {
            "Uncaught TypeError: Cannot read property 'x' of undefined",
            "Uncaught ReferenceError: game is not defined",
            "Uncaught SyntaxError: Unexpected token '}'",
            "Uncaught RangeError: Maximum call stack size exceeded",
            "Uncaught TypeError: undefined is not a function"
        },
        ["network"] = new[]
        {
            "Failed to load resource: net::ERR_CONNECTION_REFUSED",
            "Failed to load resource: net::ERR_INTERNET_DISCONNECTED",
            "Failed to load resource: net::ERR_NAME_NOT_RESOLVED",
            "WebSocket connection to 'wss://localhost/' failed: Error in connection establishment: net::ERR_CONNECTION_REFUSED"
        },
        ["webgl"] = new[]
        {
            "WebGL: CONTEXT_LOST_WEBGL",
            "WebGL: INVALID_OPERATION: drawElements: no buffer bound to attribute 0",
            "WebGL: INVALID_VALUE: texImage2D: no canvas",
            "WebGL: OUT_OF_MEMORY"
        },
        ["system"] = new[]
        {
            "FATAL: Exception 0xC0000005 (ACCESS_VIOLATION) at 0x7C34B8F2",
            "DS2 firmware corrupted – performing recovery...",
            "KERNEL_DATA_INPAGE_ERROR (0x0000007A)",
            "ACPI_BIOS_ERROR (0x000000A5)",
            "HARDWARE FAILURE: contact Nintendo support"
        }
    };

    public string GetRandomThemeClass()
    {
        if (!IsChaosModeEnabled) return "";

        // Weighted random selection
        int totalWeight = 0;
        foreach (var (_, w) in _themeClasses)
            totalWeight += w;

        int r = _rand.Next(totalWeight);
        int cumulative = 0;
        foreach (var (className, weight) in _themeClasses)
        {
            cumulative += weight;
            if (r < cumulative)
                return className;
        }
        return _themeClasses[0].className; // fallback
    }

    public bool ShouldSimulateError()
    {
        if (!IsChaosModeEnabled) return false;
        // 15% chance, but can increase over time or with intensity
        return _rand.Next(100) < 15;
    }

    public void LogRandomConsoleError(IJSRuntime js)
    {
        if (!IsChaosModeEnabled) return;

        // Cooldown to prevent spam
        if ((DateTime.Now - _lastConsoleError) < _consoleErrorCooldown)
            return;
        _lastConsoleError = DateTime.Now;

        // Pick a random error group, then a random error from that group
        var groups = new List<string>(_errorGroups.Keys);
        string group = groups[_rand.Next(groups.Count)];
        string[] errors = _errorGroups[group];
        string error = errors[_rand.Next(errors.Length)];

        // 10% chance to log a warning instead
        if (_rand.Next(10) == 0)
        {
            js.InvokeVoidAsync("console.warn", $"[ChaOS] {error} (simulated)");
        }
        else
        {
            js.InvokeVoidAsync("console.error", $"[ChaOS] {error} (simulated)");
        }

        // 5% chance to also log a stack trace
        if (_rand.Next(20) == 0)
        {
            js.InvokeVoidAsync("console.trace", "ChaOS generated trace");
        }
    }

    // Additional method for more advanced glitch effects (used by DS2 emulator)
    public (string effect, int durationMs) GetRandomGlitchEffect()
    {
        if (!IsChaosModeEnabled) return (null, 0);

        var effects = new[]
        {
            ("glitch", 200),
            ("static", 150),
            ("colorShift", 300),
            ("flicker", 100),
            ("pixelate", 400),
            ("wave", 250),
            ("vhs", 350),
            ("corrupt", 500)
        };
        var (effect, duration) = effects[_rand.Next(effects.Length)];
        return (effect, duration);
    }
}