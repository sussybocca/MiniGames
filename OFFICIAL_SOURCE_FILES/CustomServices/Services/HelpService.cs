using System.Collections.Generic;

namespace MiniGames.CustomServices.Services;

public class HelpService : IHelpService
{
    public IEnumerable<FaqItem> GetFAQs()
    {
        return new List<FaqItem>
        {
            new()
            {
                Question = "How do I load a game?",
                Answer = "Click the 'Library' button on the DS3 console, then select a game cartridge from the grid. If you don't see your game, make sure it's listed in games.json and the ROM file is in the correct folder under wwwroot/games."
            },
            new()
            {
                Question = "Why is the screen green/black?",
                Answer = "This usually means the emulator core (jsGB) failed to load or the ROM file is invalid. Check the browser console (F12) for errors. Ensure the jsGB script is loaded correctly in index.html and that your ROM is a valid GameBoy .gb file."
            },
            new()
            {
                Question = "Can I use save states?",
                Answer = "The DS3 emulator currently uses the built-in save functionality of jsGB. Game saves are stored in the browser's local storage and persist across sessions. Manual save states are not yet implemented."
            },
            new()
            {
                Question = "How do I map keyboard controls?",
                Answer = "Controls are fixed: D‑Pad arrows, A/B/X/Y correspond to GameBoy A/B/Select/Start. You cannot remap them in this version, but we plan to add customization later."
            }
        };
    }
}