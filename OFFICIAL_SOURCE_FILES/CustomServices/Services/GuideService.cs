using System.Collections.Generic;

namespace MiniGames.CustomServices.Services;

public class GuideService : IGuideService
{
    public string GetIntroduction()
    {
        return "Welcome to the Nintendo DS3 Emulator – a web‑based GameBoy emulator with a nostalgic dual‑screen design. " +
               "This guide will help you understand the features, controls, and troubleshooting tips.";
    }

    public IEnumerable<GuideSection> GetSections()
    {
        return new List<GuideSection>
        {
            new()
            {
                Title = "Getting Started",
                Content = "To start, navigate to the DS3 section from the main menu. The console will appear with a top screen (game display) and a bottom screen (touch overlay). " +
                          "Press the 'Library' button to browse available games. Select a game cartridge to load it. The game will start automatically."
            },
            new()
            {
                Title = "Controls",
                Content = "The physical buttons are mapped as follows:\n" +
                          "- D‑Pad: Move (Up, Down, Left, Right)\n" +
                          "- A / B: GameBoy A and B buttons\n" +
                          "- X: Select\n" +
                          "- Y: Start\n" +
                          "- L / R: Not used (reserved)\n" +
                          "- Start / Select: Duplicate of X/Y for convenience.\n\n" +
                          "You can also use the on‑screen touch overlay (bottom screen) for mouse/touch input, though it's primarily for future touch‑enabled games."
            },
            new()
            {
                Title = "Saving & Loading",
                Content = "Game saves are automatically stored in your browser's local storage. When you close the tab or refresh, your progress is preserved. " +
                          "To clear saves, use your browser's developer tools to delete local storage for this site."
            },
            new()
            {
                Title = "Adding Your Own Games",
                Content = "To add a new GameBoy ROM:\n" +
                          "1. Place the .gb file in the wwwroot/games/{folder}/ directory (create a new folder named after the game).\n" +
                          "2. Edit data/games.json and add an entry: [\"filename.gb\", \"Display Name\", \"folder-name\"].\n" +
                          "3. Optionally add a cartridge image at wwwroot/images/cartridges/folder-name.png.\n" +
                          "4. Rebuild and reload the app."
            },
            new()
            {
                Title = "Troubleshooting",
                Content = "**Green/black screen:** Check that jsGB is loaded (browser console). Ensure ROM is valid and path is correct.\n" +
                          "**Game not appearing in library:** Verify games.json syntax and that the folder name matches exactly.\n" +
                          "**Controls not working:** Click on the game canvas to focus it, then try again."
            }
        };
    }
}