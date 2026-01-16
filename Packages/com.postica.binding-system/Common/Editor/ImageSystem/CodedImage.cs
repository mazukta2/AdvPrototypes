using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Postica.Common
{
    internal interface IByteArray
    {
        byte[] Bytes { get; }
        bool IsCompressed { get; }
    }

    public abstract class CodedImage : IByteArray
    {
        private static readonly Dictionary<string, Texture2D> _cachedTextures = new Dictionary<string, Texture2D>();

        protected Texture2D _texture;
        private string _typeIdentifier;
        public virtual string Identifier => _typeIdentifier;
        public virtual TextureFormat Format => _texture ? _texture.format : TextureFormat.RGBA32;
        protected virtual bool UseMipMaps => Size.width == Size.height && Size.width >= 64;
        protected internal abstract byte[] Bytes { get; }
        public abstract (int width, int height) Size { get; }
        protected internal abstract bool IsPNGorJPEG { get; }
        public Texture2D Texture
        {
            get
            {
                if (!_texture 
                    && (!_cachedTextures.TryGetValue(Identifier, out _texture)
                        || !_texture))
                {
                    var (width, height) = Size;
                    _texture = new Texture2D(width, height, Format, UseMipMaps);
                    if (IsPNGorJPEG)
                    {
                        _texture.LoadImage(Bytes);
                        //_texture.Apply();
                    }
                    else
                    {
                        _texture.LoadRawTextureData(Bytes);
                        _texture.Apply();
                    }
                    _cachedTextures[Identifier] = _texture;
                }
                return _texture;
            }
        }

        byte[] IByteArray.Bytes => Bytes;

        public bool IsCompressed => IsPNGorJPEG;

        public CodedImage()
        {
            _typeIdentifier = GetType().Name;
        }

        public static implicit operator Texture2D(CodedImage imageBytes)
        {
            return imageBytes.Texture;
        }

        public static implicit operator Texture(CodedImage imageBytes)
        {
            return imageBytes.Texture;
        }
    }
}