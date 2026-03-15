using System.Collections.Concurrent;
using System.Text.Json;

namespace Sandblox.Models;

public class World
{
    private readonly ConcurrentDictionary<(int cx, int cy, int cz), Chunk> _chunks = new();
    private readonly Noise _noise = new();

    public Chunk GetOrGenerateChunk(int cx, int cy, int cz)
    {
        return _chunks.GetOrAdd((cx, cy, cz), key =>
        {
            var chunk = new Chunk(key.cx, key.cy, key.cz);
            GenerateChunkTerrain(chunk);
            return chunk;
        });
    }

    private void GenerateChunkTerrain(Chunk chunk)
    {
        // Simple terrain: grass, dirt, stone with caves and ores
        for (int x = 0; x < Chunk.Width; x++)
        {
            for (int z = 0; z < Chunk.Depth; z++)
            {
                int worldX = chunk.ChunkX * Chunk.Width + x;
                int worldZ = chunk.ChunkZ * Chunk.Depth + z;
                double heightBase = _noise.Evaluate(worldX * 0.01, worldZ * 0.01) * 20 + 64;
                int groundHeight = (int)heightBase;

                for (int y = 0; y < Chunk.Height; y++)
                {
                    int worldY = chunk.ChunkY * Chunk.Height + y;
                    BlockType type = BlockType.Air;

                    if (worldY <= groundHeight)
                    {
                        if (worldY == groundHeight) type = BlockType.Grass;
                        else if (worldY > groundHeight - 4) type = BlockType.Dirt;
                        else type = BlockType.Stone;

                        // Add ores randomly
                        if (type == BlockType.Stone)
                        {
                            double oreNoise = _noise.Evaluate(worldX * 0.05, worldY * 0.05, worldZ * 0.05);
                            if (oreNoise > 0.8) type = BlockType.CoalOre;
                            else if (oreNoise > 0.7) type = BlockType.IronOre;
                            else if (oreNoise > 0.65) type = BlockType.GoldOre;
                            else if (oreNoise > 0.62) type = BlockType.DiamondOre;
                        }
                    }
                    else if (worldY <= 62)
                    {
                        type = BlockType.Water; // sea level
                    }

                    chunk.SetBlock(x, y, z, type);
                }
            }
        }
    }

    public BlockType GetBlock(int x, int y, int z)
    {
        var (cx, cy, cz) = GetChunkCoords(x, y, z);
        if (!_chunks.TryGetValue((cx, cy, cz), out var chunk))
            return BlockType.Air;
        return chunk.GetBlock(x - cx * Chunk.Width, y - cy * Chunk.Height, z - cz * Chunk.Depth);
    }

    public void SetBlock(int x, int y, int z, BlockType type)
    {
        var (cx, cy, cz) = GetChunkCoords(x, y, z);
        var chunk = GetOrGenerateChunk(cx, cy, cz);
        chunk.SetBlock(x - cx * Chunk.Width, y - cy * Chunk.Height, z - cz * Chunk.Depth, type);
    }

    public Chunk? GetChunkContaining(int x, int y, int z)
    {
        var (cx, cy, cz) = GetChunkCoords(x, y, z);
        return _chunks.TryGetValue((cx, cy, cz), out var chunk) ? chunk : null;
    }

    private static (int, int, int) GetChunkCoords(int x, int y, int z) =>
        (x >> 4, y >> 4, z >> 4); // divide by 16

    public byte[] Serialize()
    {
        var chunksData = _chunks.ToDictionary(
            kv => $"{kv.Key.cx},{kv.Key.cy},{kv.Key.cz}",
            kv => SerializeChunk(kv.Value)
        );
        return JsonSerializer.SerializeToUtf8Bytes(chunksData);
    }

    public void Deserialize(byte[] data)
    {
        var chunksData = JsonSerializer.Deserialize<Dictionary<string, byte[]>>(data);
        if (chunksData == null) return;
        foreach (var kv in chunksData)
        {
            var parts = kv.Key.Split(',').Select(int.Parse).ToArray();
            var chunk = DeserializeChunk(kv.Value, parts[0], parts[1], parts[2]);
            _chunks[(parts[0], parts[1], parts[2])] = chunk;
        }
    }

    private static byte[] SerializeChunk(Chunk chunk)
    {
        // Simple run-length encoding for compression
        var blocks = new List<byte>();
        BlockType lastType = BlockType.Air;
        int run = 0;
        for (int y = 0; y < Chunk.Height; y++)
            for (int x = 0; x < Chunk.Width; x++)
                for (int z = 0; z < Chunk.Depth; z++)
                {
                    var type = chunk.GetBlock(x, y, z);
                    if (type == lastType && run < 255)
                    {
                        run++;
                    }
                    else
                    {
                        if (run > 0)
                        {
                            blocks.Add((byte)lastType);
                            blocks.Add((byte)run);
                        }
                        lastType = type;
                        run = 1;
                    }
                }
        if (run > 0)
        {
            blocks.Add((byte)lastType);
            blocks.Add((byte)run);
        }
        return blocks.ToArray();
    }

    private static Chunk DeserializeChunk(byte[] data, int cx, int cy, int cz)
    {
        var chunk = new Chunk(cx, cy, cz);
        int index = 0;
        for (int y = 0; y < Chunk.Height; y++)
            for (int x = 0; x < Chunk.Width; x++)
                for (int z = 0; z < Chunk.Depth;)
                {
                    if (index >= data.Length) break;
                    var type = (BlockType)data[index++];
                    var run = data[index++];
                    for (int i = 0; i < run; i++)
                    {
                        if (z >= Chunk.Depth) { z = 0; x++; if (x >= Chunk.Width) { x = 0; y++; } }
                        chunk.SetBlock(x, y, z, type);
                        z++;
                    }
                }
        return chunk;
    }
}