using System.Collections.Generic;

namespace MiniGames.CsLists;

public class GameInfo
{
    public string Id { get; set; }          // Unique identifier (used in routes)
    public string Name { get; set; }         // Display name
    public string Description { get; set; }  // Short description
    public string Icon { get; set; }         // Emoji or icon class
    public string Route { get; set; }        // Relative URL to play
    public string Color { get; set; }         // Theme color (CSS color)
    public bool IsFeatured { get; set; }      // Whether to highlight
}

public static class GameList
{
    public static List<GameInfo> GetAllGames() => new()
    {
        new GameInfo
        {
            Id = "Elemental-sandbox",
            Name = "Elemental Sandbox",
            Description = "Build, destroy, and experiment with physics in this immersive sandbox.",
            Icon = "🌊",
            Route = "/elemental-sandbox",
            Color = "#4a90e2",
            IsFeatured = true
        },
        new GameInfo
        {
            Id = "crusher-sandbox",
            Name = "Crusher Sandbox",
            Description = "Crush cars, trees, houses, and dig holes in this destruction playground.",
            Icon = "💥",
            Route = "/crusher-sandbox",
            Color = "#dc143c",
            IsFeatured = true
        },
        new GameInfo
        {
            Id = "physics-sandbox",
            Name = "Physics Sandbox",
            Description = "Tweak gravity, friction, wind, and spawn thousands of bodies – stress-test your CPU!",
            Icon = "⚡",
            Route = "/physics-sandbox",
            Color = "#ffaa00",
            IsFeatured = true
        },
        new GameInfo
        {
            Id = "gameboy3",
            Name = "GameBoy 3",
            Description = "Classic GameBoy emulator with enhanced graphics.",
            Icon = "🎮",
            Route = "/gameboy3/play",
            Color = "#8b5a2b",
            IsFeatured = false
        },
        new GameInfo
        {
            Id = "universal-modder",
            Name = "Universal Modder",
            Description = "Modify any game with Lua scripts.",
            Icon = "🛠️",
            Route = "/emulator/universal-modder",
            Color = "#dc143c",
            IsFeatured = false
        }
    };
}