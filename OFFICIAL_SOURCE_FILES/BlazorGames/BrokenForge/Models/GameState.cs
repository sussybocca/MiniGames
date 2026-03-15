using System.Collections.Generic;

namespace MiniGames.BlazorGames.BrokenForge.Models
{
    public class GameState
    {
        public Player Player { get; set; } = new();
        public List<Enemy> Enemies { get; set; } = new();
        public TileType[,] World { get; set; }
        public int WorldWidth { get; set; } = 100;
        public int WorldHeight { get; set; } = 100;
    }

    public class Player
    {
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int Coins { get; set; } = 50;
        public List<Item> Inventory { get; set; } = new();
        public Weapon EquippedWeapon { get; set; }
        public int PositionX { get; set; } = 50;
        public int PositionY { get; set; } = 50;
    }

    public class Enemy
    {
        public int Health { get; set; } = 30;
        public int MaxHealth { get; set; } = 30;
        public int Attack { get; set; } = 5;
        public int Defense { get; set; } = 2;
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public bool IsAlive => Health > 0;
    }

    public class Item
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public class Weapon : Item
    {
        public int Damage { get; set; }
        public int Durability { get; set; }
    }
}