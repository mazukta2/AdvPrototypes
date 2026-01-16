using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Postica.Common
{
    public class GenericImage : CodedImage
    {
        private TextureFormat? _format;
        private bool? _hasMipmaps;

        public sealed override (int width, int height) Size { get; }
        public override string Identifier { get; }
        internal protected override byte[] Bytes { get; }
        internal protected override bool IsPNGorJPEG { get; }
        public override TextureFormat Format => _format ?? base.Format;
        protected override bool UseMipMaps => _hasMipmaps ?? base.UseMipMaps;

        public GenericImage(string id, int width, int height, byte[] bytes): this(id, width, height, false, bytes) { }

        public GenericImage(string id, int width, int height, bool isPng, TextureFormat? format, byte[] bytes, bool? useMipmaps = null)
        {
            Size = (width, height);
            Bytes = bytes;
            Identifier = id;
            IsPNGorJPEG = isPng;
            _format = format;
            _hasMipmaps = useMipmaps;
            if (!useMipmaps.HasValue && isPng && width == height && width >= 32)
            {
                _hasMipmaps = true;
            }
        }

        public GenericImage(string id, int width, int height, bool isPng, byte[] bytes) : this(id, width, height, isPng, null, bytes)
        { }

        public GenericImage(string id, byte[] bytes)
        {
            var side = Mathf.CeilToInt(Mathf.Sqrt(bytes.Length / 4));
            Size = (side, side);
            Bytes = bytes;
            Identifier = id;
            IsPNGorJPEG = false;
        }

        public static implicit operator GenericImage(byte[] array)
        {
            return new GenericImage(array.GetHashCode().ToString(), array);
        }
    }
}