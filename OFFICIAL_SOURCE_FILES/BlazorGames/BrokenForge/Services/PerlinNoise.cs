using System;

namespace MiniGames.BlazorGames.BrokenForge.Services
{
    public class PerlinNoise
    {
        private readonly int[] permutation;
        private readonly int[] p;

        public PerlinNoise(int seed)
        {
            var rand = new Random(seed);
            permutation = new int[256];
            for (int i = 0; i < 256; i++)
                permutation[i] = i;

            for (int i = 255; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                (permutation[i], permutation[j]) = (permutation[j], permutation[i]);
            }

            p = new int[512];
            for (int i = 0; i < 512; i++)
                p[i] = permutation[i % 256];
        }

        private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static double Lerp(double t, double a, double b) => a + t * (b - a);
        private static double Grad(int hash, double x, double y, double z)
        {
            int h = hash & 15;
            double u = h < 8 ? x : y;
            double v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        public double Noise(double x, double y, double z)
        {
            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;
            int zi = (int)Math.Floor(z) & 255;

            double xf = x - Math.Floor(x);
            double yf = y - Math.Floor(y);
            double zf = z - Math.Floor(z);

            double u = Fade(xf);
            double v = Fade(yf);
            double w = Fade(zf);

            int aaa = p[p[p[xi] + yi] + zi];
            int aba = p[p[p[xi] + yi + 1] + zi];
            int aab = p[p[p[xi] + yi] + zi + 1];
            int abb = p[p[p[xi] + yi + 1] + zi + 1];
            int baa = p[p[p[xi + 1] + yi] + zi];
            int bba = p[p[p[xi + 1] + yi + 1] + zi];
            int bab = p[p[p[xi + 1] + yi] + zi + 1];
            int bbb = p[p[p[xi + 1] + yi + 1] + zi + 1];

            double x1, x2, y1, y2;
            x1 = Lerp(u, Grad(aaa, xf, yf, zf), Grad(baa, xf - 1, yf, zf));
            x2 = Lerp(u, Grad(aba, xf, yf - 1, zf), Grad(bba, xf - 1, yf - 1, zf));
            y1 = Lerp(v, x1, x2);

            x1 = Lerp(u, Grad(aab, xf, yf, zf - 1), Grad(bab, xf - 1, yf, zf - 1));
            x2 = Lerp(u, Grad(abb, xf, yf - 1, zf - 1), Grad(bbb, xf - 1, yf - 1, zf - 1));
            y2 = Lerp(v, x1, x2);

            return (Lerp(w, y1, y2) + 1) / 2;
        }
    }
}