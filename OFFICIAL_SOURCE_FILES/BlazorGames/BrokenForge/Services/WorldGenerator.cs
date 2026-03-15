using MiniGames.BlazorGames.BrokenForge.Models;

namespace MiniGames.BlazorGames.BrokenForge.Services
{
    public class WorldGenerator
    {
        private readonly int _width;
        private readonly int _height;
        private readonly PerlinNoise _noise;
        private readonly double _scale;

        public WorldGenerator(int width, int height, int seed, double scale = 0.01)
        {
            _width = width;
            _height = height;
            _noise = new PerlinNoise(seed);
            _scale = scale;
        }

        public TileType[,] Generate()
        {
            var world = new TileType[_width, _height];
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    double nx = x * _scale;
                    double ny = y * _scale;
                    double nz = 0.0;
                    double value = _noise.Noise(nx, ny, nz);
                    world[x, y] = EvaluateTile(value);
                }
            }
            return world;
        }

        private TileType EvaluateTile(double value)
        {
            if (value < 0.2) return TileType.DeepWater;
            if (value < 0.3) return TileType.Water;
            if (value < 0.4) return TileType.Sand;
            if (value < 0.6) return TileType.Grass;
            if (value < 0.8) return TileType.Forest;
            if (value < 0.9) return TileType.Stone;
            return TileType.Mountain;
        }
    }
}