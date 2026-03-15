using System.Collections.Generic;
using System.Linq;

namespace Sandblox.Models;

public class Chunk
{
    public const int Width = 16;
    public const int Height = 256;
    public const int Depth = 16;

    public int ChunkX { get; }
    public int ChunkY { get; }
    public int ChunkZ { get; }
    public string Key => $"chunk_{ChunkX}_{ChunkY}_{ChunkZ}";

    private BlockType[,,] _blocks = new BlockType[Width, Height, Depth];
    public bool NeedsRender { get; set; } = true; // mark for re-render on change

    public Chunk(int cx, int cy, int cz)
    {
        ChunkX = cx; ChunkY = cy; ChunkZ = cz;
    }

    public BlockType GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
            return BlockType.Air;
        return _blocks[x, y, z];
    }

    public void SetBlock(int x, int y, int z, BlockType type)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
            return;
        _blocks[x, y, z] = type;
        NeedsRender = true;
    }

    public IEnumerable<Block> GetNonAirBlocks()
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                for (int z = 0; z < Depth; z++)
                {
                    var type = _blocks[x, y, z];
                    if (type != BlockType.Air)
                    {
                        yield return new Block(
                            ChunkX * Width + x,
                            ChunkY * Height + y,
                            ChunkZ * Depth + z,
                            type
                        );
                    }
                }
    }

    public void Fill(BlockType type)
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                for (int z = 0; z < Depth; z++)
                    _blocks[x, y, z] = type;
    }
}