namespace Sandblox.Models;

public class Player
{
    public float X { get; set; } = 0;
    public float Y { get; set; } = 80; // start above ground
    public float Z { get; set; } = 0;
    public float Yaw { get; set; } = 0;
    public float Pitch { get; set; } = 0;
    public int Health { get; set; } = 20;
    public int Hunger { get; set; } = 20;
    public Inventory Inventory { get; } = new();

    public int ChunkX => (int)X >> 4;
    public int ChunkY => (int)Y >> 4;
    public int ChunkZ => (int)Z >> 4;
}