using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Graphs.Styles;

namespace Postica.Common
{
    public class EffectImage : CodedImage
    {
        public interface IEffect
        {
            string Name { get; }
            byte[] Apply(TextureFormat format, byte[] source);
        }

        private static readonly Dictionary<string, byte[]> _cachedBytes = new Dictionary<string, byte[]>();

        private byte[] _bytes;
        private CodedImage _image;
        private IEffect[] _effects;

        protected internal override byte[] Bytes
        {
            get
            {
                if (_bytes == null && !_cachedBytes.TryGetValue(Identifier, out _bytes))
                {
                    byte[] source;
                    if ((_image as IByteArray).IsCompressed)
                    {
                        source = _image.Texture.GetRawTextureData();
                    }
                    else
                    {
                        source = (_image as IByteArray).Bytes;
                    }
                    foreach (var effect in _effects)
                    {
                        source = effect.Apply(Format, source);
                    }
                    _bytes = source;
                    _cachedBytes[Identifier] = _bytes;
                }
                return _bytes;
            }
        }

        protected internal override bool IsPNGorJPEG => false;
        public override string Identifier { get; }
        public override (int width, int height) Size { get; }
        public override TextureFormat Format => _image.Format;

        public EffectImage(CodedImage image, IEffect effect)
        {
            Identifier = string.Concat("Effect_", effect.Name, "_", image.Identifier);
            Size = image.Size;
            _image = image;
            _effects = new IEffect[] { effect };
        }

        public EffectImage(CodedImage image, params IEffect[] effects)
        {
            Identifier = string.Concat("Effect_", string.Join("_", effects.Select(e => e.Name)), "_", image.Identifier);
            Size = image.Size;
            _image = image;
            _effects = effects;
        }
    }
}