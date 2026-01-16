using UnityEngine;

namespace Postica.Common
{
    /// <summary>
    /// Utility class to perform various channels operations, mainly for textures
    /// </summary>
    internal struct Channels
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public Channels(int r, int g, int b, int a)
        {
            this.r = (byte)r;
            this.g = (byte)g;
            this.b = (byte)b;
            this.a = (byte)a;
        }

        public static implicit operator Channels(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.ARGB32: return new Channels(1, 2, 3, 0);
                case TextureFormat.ARGB4444: return new Channels(1, 2, 3, 0);
                case TextureFormat.BGRA32: return new Channels(2, 1, 0, 3);
                case TextureFormat.R16: 
                case TextureFormat.R8: return new Channels(0, 0, 0, 0);
                case TextureFormat.RGB24: return new Channels(0, 1, 2, 3);
                case TextureFormat.RGBA32: return new Channels(0, 1, 2, 3);

                default: return new Channels(0, 1, 2, 3);
            }
        }
    }
}
