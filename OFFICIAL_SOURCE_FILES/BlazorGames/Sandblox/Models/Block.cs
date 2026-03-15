namespace Sandblox.Models;

public class Block
{
    public int X { get; }
    public int Y { get; }
    public int Z { get; }
    public BlockType Type { get; set; }

    public Block(int x, int y, int z, BlockType type)
    {
        X = x; Y = y; Z = z; Type = type;
    }
}