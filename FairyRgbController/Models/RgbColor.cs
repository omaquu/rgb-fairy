using System;

namespace FairyRgbController.Models
{
    public readonly struct RgbColor
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public RgbColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
    }
}
