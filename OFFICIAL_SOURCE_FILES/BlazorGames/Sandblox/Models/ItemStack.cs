namespace Sandblox.Models;

public class ItemStack
{
    public BlockType Type { get; set; }
    public int Count { get; set; }

    public ItemStack(BlockType type, int count)
    {
        Type = type;
        Count = count;
    }
}