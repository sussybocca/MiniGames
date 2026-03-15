namespace Sandblox.Models;

public enum BlockType : byte
{
    Air = 0,
    Grass,
    Dirt,
    Stone,
    Cobblestone,
    Bedrock,
    Water,
    Sand,
    Gravel,
    Wood,
    Leaves,
    Glass,
    Brick,
    Planks,
    CoalOre,
    IronOre,
    GoldOre,
    DiamondOre,
    CraftingTable,
    Torch
}

public static class BlockTypeExtensions
{
    public static bool IsSolid(this BlockType type) => type != BlockType.Air && type != BlockType.Water && type != BlockType.Torch;
    public static bool IsTransparent(this BlockType type) => type == BlockType.Air || type == BlockType.Water || type == BlockType.Glass || type == BlockType.Leaves || type == BlockType.Torch;
    public static bool IsBlock(this BlockType type) => type != BlockType.Air; // for item usage
}