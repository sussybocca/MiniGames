using System.Text.Json;
using MiniGames.Models;

namespace MiniGames.Services;

public class GameService
{
    private readonly HttpClient _http;
    private List<GameInfo>? _games;

    public GameService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<GameInfo>> GetGamesAsync()
    {
        if (_games != null) return _games;

        var json = await _http.GetStringAsync("data/games.json");

        // Try to parse as new object format first
        try
        {
            var entries = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);
            if (entries != null && entries.Count > 0)
            {
                _games = entries.Select(e => MapFromObject(e)).ToList();
                return _games;
            }
        }
        catch (JsonException)
        {
            // Fall back to old array format
        }

        // Old array format: [gamePath, name, folder]
        var arrayEntries = JsonSerializer.Deserialize<List<string[]>>(json);
        _games = arrayEntries?
            .Where(e => e != null && e.Length > 0)
            .Select(e => new GameInfo
            {
                EntryFile = e[0],
                Name = e.Length > 1 ? e[1] : string.Empty,
                Folder = e.Length > 2 ? e[2] : (e.Length > 1 ? e[1] : string.Empty)
            })
            .ToList() ?? new List<GameInfo>();

        return _games;
    }

    private GameInfo MapFromObject(Dictionary<string, JsonElement> obj)
    {
        var game = new GameInfo();

        foreach (var kv in obj)
        {
            switch (kv.Key)
            {
                case "gamePath": game.EntryFile = kv.Value.GetString() ?? ""; break;
                case "name": game.Name = kv.Value.GetString() ?? ""; break;
                case "folder": game.Folder = kv.Value.GetString() ?? ""; break;
                case "platform": game.Platform = kv.Value.GetString(); break;
                case "developer": game.Developer = kv.Value.GetString(); break;
                case "publisher": game.Publisher = kv.Value.GetString(); break;
                case "releaseYear": game.ReleaseYear = kv.Value.TryGetInt32(out var y) ? y : null; break;
                case "genre": game.Genre = kv.Value.GetString(); break;
                case "description": game.Description = kv.Value.GetString(); break;
                case "coverImage": game.CoverImage = kv.Value.GetString(); break;
                case "romType": game.RomType = kv.Value.GetString(); break;
                case "tags":
                    if (kv.Value.ValueKind == JsonValueKind.Array)
                    {
                        var tags = new List<string>();
                        foreach (var item in kv.Value.EnumerateArray())
                        {
                            tags.Add(item.GetString() ?? "");
                        }
                        game.Tags = tags;
                    }
                    break;
            }
        }

        // Ensure required fields have fallbacks
        if (string.IsNullOrEmpty(game.Folder) && !string.IsNullOrEmpty(game.Name))
            game.Folder = game.Name.ToLowerInvariant().Replace(" ", "-");

        return game;
    }
}