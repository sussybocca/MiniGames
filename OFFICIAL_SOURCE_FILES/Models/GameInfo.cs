namespace MiniGames.Models;

public class GameInfo
{
    // Core fields (always present)
    public string EntryFile { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;

    // New optional fields for enhanced metadata
    public string? Platform { get; set; }
    public string? Developer { get; set; }
    public string? Publisher { get; set; }
    public int? ReleaseYear { get; set; }
    public string? Genre { get; set; }
    public string? Description { get; set; }
    public string? CoverImage { get; set; }      // Alternative to /images/cartridges/{Folder}.png
    public string? RomType { get; set; }          // e.g., "gb", "nes", "html"
    public string? System { get; set; }           // e.g., "gba", "ps1", "flash" – for UFE
    public List<string>? Tags { get; set; }       // For categorization

    // Computed URL (remains unchanged)
    public string GamePath => $"/games/{Folder}/{EntryFile}";
}