using Sandblox.Models;
using System.Collections.Concurrent;

namespace Sandblox.Services;

public class GameService
{
    private readonly World _world;
    private readonly Player _player;
    private readonly WorldPersistence _persistence;
    private ThreeJsInterop? _threeJs;
    private readonly ConcurrentDictionary<string, DateTime> _lastChunkAccess = new();
    private readonly CancellationTokenSource _cts = new();
    private const int RenderDistance = 8; // chunks

    public GameService(WorldPersistence persistence)
    {
        _world = new World();
        _player = new Player();
        _persistence = persistence;
    }

    public async Task InitializeAsync(ThreeJsInterop threeJs)
    {
        _threeJs = threeJs;
        await LoadWorldAsync();
        _ = Task.Run(ChunkManagementLoop);
    }

    private async Task LoadWorldAsync()
    {
        var worldData = await _persistence.LoadWorldAsync();
        if (worldData != null)
            _world.Deserialize(worldData);
        else
            GenerateSpawnArea();
    }

    private void GenerateSpawnArea()
    {
        for (int cx = -2; cx <= 2; cx++)
            for (int cz = -2; cz <= 2; cz++)
                _world.GetOrGenerateChunk(cx, 0, cz);
    }

    private async Task ChunkManagementLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            await Task.Delay(500);
            var playerChunk = (_player.ChunkX, _player.ChunkY, _player.ChunkZ);
            var toLoad = new HashSet<(int, int, int)>();
            for (int dx = -RenderDistance; dx <= RenderDistance; dx++)
                for (int dz = -RenderDistance; dz <= RenderDistance; dz++)
                    for (int dy = -2; dy <= 2; dy++) // vertical range
                    {
                        int cx = playerChunk.Item1 + dx;
                        int cy = playerChunk.Item2 + dy;
                        int cz = playerChunk.Item3 + dz;
                        if (cy >= 0 && cy <= 15) // world height in chunks
                            toLoad.Add((cx, cy, cz));
                    }

            // Load missing chunks
            foreach (var coords in toLoad)
            {
                var chunk = _world.GetOrGenerateChunk(coords.Item1, coords.Item2, coords.Item3);
                if (chunk.NeedsRender)
                {
                    await SendChunkToRenderer(chunk);
                    chunk.NeedsRender = false;
                }
                _lastChunkAccess[ChunkKey(coords)] = DateTime.UtcNow;
            }

            // Unload distant chunks
            var toUnload = _lastChunkAccess.Keys
                .Where(key => !toLoad.Contains(ParseChunkKey(key)) &&
                              DateTime.UtcNow - _lastChunkAccess[key] > TimeSpan.FromSeconds(10))
                .ToList();
            foreach (var key in toUnload)
            {
                if (_threeJs != null)
                    await _threeJs.RemoveChunk(key);
                _lastChunkAccess.TryRemove(key, out _);
            }
        }
    }

    private async Task SendChunkToRenderer(Chunk chunk)
    {
        if (_threeJs == null) return;
        var blocks = chunk.GetNonAirBlocks()
            .Select(b => new { b.X, b.Y, b.Z, Type = (int)b.Type })
            .ToArray();
        await _threeJs.UpdateChunk(chunk.Key, blocks);
    }

    public void BreakBlock(int x, int y, int z)
    {
        var block = _world.GetBlock(x, y, z);
        if (block == BlockType.Air) return;
        _world.SetBlock(x, y, z, BlockType.Air);
        _player.Inventory.AddItem(new ItemStack(block, 1));
        NotifyChunkUpdate(x, y, z);
    }

    public void PlaceBlock(int x, int y, int z, int face, int hotbarSlot)
    {
        var item = _player.Inventory.GetHotbarItem(hotbarSlot);
        if (item == null || item.Count <= 0 || !item.Type.IsBlock()) return;

        var placePos = (x, y, z) switch
        {
            (_, _, _) when face == 0 => (x, y - 1, z), // bottom
            (_, _, _) when face == 1 => (x, y + 1, z), // top
            (_, _, _) when face == 2 => (x, y, z - 1), // north
            (_, _, _) when face == 3 => (x, y, z + 1), // south
            (_, _, _) when face == 4 => (x - 1, y, z), // west
            (_, _, _) when face == 5 => (x + 1, y, z), // east
            _ => (x, y, z)
        };

        if (_world.GetBlock(placePos.x, placePos.y, placePos.z) != BlockType.Air)
            return;

        _world.SetBlock(placePos.x, placePos.y, placePos.z, (BlockType)item.Type);
        _player.Inventory.RemoveItem(new ItemStack(item.Type, 1));
        NotifyChunkUpdate(placePos.x, placePos.y, placePos.z);
    }

    private void NotifyChunkUpdate(int x, int y, int z)
    {
        var chunk = _world.GetChunkContaining(x, y, z);
        if (chunk != null)
        {
            chunk.NeedsRender = true;
            _ = SendChunkToRenderer(chunk); // fire and forget
        }
    }

    public string GetHotbarItemName(int slot)
    {
        var item = _player.Inventory.GetHotbarItem(slot);
        return item?.Type.ToString() ?? "Empty";
    }

    public async Task SaveWorldAsync()
    {
        await _persistence.SaveWorldAsync(_world.Serialize());
    }

    private static string ChunkKey((int, int, int) coords) => $"chunk_{coords.Item1}_{coords.Item2}_{coords.Item3}";
    private static (int, int, int) ParseChunkKey(string key)
    {
        var parts = key.Split('_');
        return (int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]));
    }
}