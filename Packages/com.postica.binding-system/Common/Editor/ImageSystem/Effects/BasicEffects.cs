using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Postica.Common
{
    public static class BasicEffects
    {
        #region [  UTILITY METHODS  ]

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte Clamp(byte a)
        {
            return a < 0 ? (byte)0 : a > byte.MaxValue ? byte.MaxValue : a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte Clamp(int a) => a < 0 ? (byte)0 : a > byte.MaxValue ? byte.MaxValue : (byte)a;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte Clamp(float a) => a < 0 ? (byte)0 : a > byte.MaxValue ? byte.MaxValue : (byte)a;

        #endregion

        #region [  EXTENSION METHODS  ]

        public static CodedImage AddEffects(this CodedImage image, params EffectImage.IEffect[] effects)
        {
            return new EffectImage(image, effects);
        }

        public static CodedImage MultiplyBy(this CodedImage image, Color color) 
            => new EffectImage(image, Multiply(color));
        public static CodedImage AddColor(this CodedImage image, Color color) 
            => new EffectImage(image, Add(color));
        public static CodedImage Invert(this CodedImage image, bool invertAlpha = false) 
            => new EffectImage(image, Invert(invertAlpha));
        
        public static CodedImage Resize(this CodedImage image, int width, int height)
        {
            return new ResizedImage(image, width, height);
        }

        public class ResizedImage : CodedImage
        {
            public ResizedImage(CodedImage image, int width, int height)
            {
                _texture = ResizeTextureGPU(image.Texture, width, height);
                Size = (width, height);
                Identifier = string.Concat("Resized_", width, "x", height, "_", image.Identifier);
            }
            
            private static Texture2D ResizeTextureGPU(Texture2D source, int targetWidth, int targetHeight)
            {
                // Create a temporary RenderTexture with bilinear filtering
                RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
                rt.filterMode = FilterMode.Bilinear;
    
                // Copy and scale the source texture into the RenderTexture
                Graphics.Blit(source, rt);
    
                // Backup the currently active RenderTexture and set our RT as active
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
    
                // Read the pixels into a new Texture2D
                Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);
                result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
                result.Apply();
    
                // Restore the active RenderTexture and release the temporary one
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
    
                return result;
            }

            public override string Identifier { get; }
            protected internal override byte[] Bytes { get; }
            public override (int width, int height) Size { get; }
            protected internal override bool IsPNGorJPEG { get; }
        }

        #endregion

        #region [  FACTORY METHODS  ]

        public static EffectImage.IEffect Multiply(Color color) => new MultiplyEffect(color);
        public static EffectImage.IEffect Add(Color color) => new AdditiveEffect(color);
        public static EffectImage.IEffect Invert(bool invertAlpha) => new InvertEffect(invertAlpha);

        #endregion

        #region [  EFFECTS DEFINITIONS  ]

        class MultiplyEffect : EffectImage.IEffect
        {
            private Color _color;

            public string Name { get; }

            internal MultiplyEffect(Color color)
            {
                _color = color;
                Name = string.Concat(nameof(MultiplyEffect), color.ToString());
            }

            public byte[] Apply(TextureFormat format, byte[] source)
            {
                var length = source.Length;
                var bytes = new byte[length];
                var color = _color.AdaptFormat(format);
                for (int i = 0; i < length; i += 4)
                {
                    bytes[i] = Clamp(color.r * source[i]);             // red
                    bytes[i + 1] = Clamp(color.g * source[i + 1]);     // green
                    bytes[i + 2] = Clamp(color.b * source[i + 2]);     // blue
                    bytes[i + 3] = Clamp(color.a * source[i + 3]);     // alpha
                }

                return bytes;
            }
        }

        class AdditiveEffect : EffectImage.IEffect
        {
            private Color _color;

            public string Name { get; }

            internal AdditiveEffect(Color color)
            {
                _color = color;
                Name = string.Concat(nameof(AdditiveEffect), color.ToString());
            }

            public byte[] Apply(TextureFormat format, byte[] source)
            {
                var length = source.Length;
                var bytes = new byte[length];
                var color = _color.AdaptFormat(format);
                var r = Clamp(color.r * 255);
                var g = Clamp(color.g * 255);
                var b = Clamp(color.b * 255);
                var a = Clamp(color.a * 255);
                for (int i = 0; i < length; i += 4)
                {
                    bytes[i] = Clamp(r + source[i]);             // red
                    bytes[i + 1] = Clamp(g + source[i + 1]);     // green
                    bytes[i + 2] = Clamp(b + source[i + 2]);     // blue
                    bytes[i + 3] = Clamp(a + source[i + 3]);     // alpha
                }

                return bytes;
            }
        }

        class InvertEffect : EffectImage.IEffect
        {
            private bool _invertAlpha;

            public string Name { get; }

            internal InvertEffect(bool invertAlpha)
            {
                _invertAlpha = invertAlpha;
                Name = string.Concat(nameof(InvertEffect), invertAlpha ? "_wA" : string.Empty);
            }

            public byte[] Apply(TextureFormat format, byte[] source)
            {
                var max = byte.MaxValue;
                var length = source.Length;
                var bytes = new byte[length];
                Channels offsets = format;
                if (_invertAlpha)
                {
                    for (int i = 0; i < length; i += 4)
                    {
                        bytes[i + offsets.r] = (byte)(max - source[i + offsets.r]);     // red
                        bytes[i + offsets.g] = (byte)(max - source[i + offsets.g]);     // green
                        bytes[i + offsets.b] = (byte)(max - source[i + offsets.b]);     // blue
                        bytes[i + offsets.a] = (byte)(max - source[i + offsets.a]);     // alpha
                    }
                }
                else
                {
                    for (int i = 0; i < length; i += 4)
                    {
                        bytes[i + offsets.r] = (byte)(max - source[i + offsets.r]);     // red
                        bytes[i + offsets.g] = (byte)(max - source[i + offsets.g]);     // green
                        bytes[i + offsets.b] = (byte)(max - source[i + offsets.b]);     // blue
                        bytes[i + offsets.a] = source[i + offsets.a];                   // alpha
                    }
                }

                return bytes;
            }
        }

        #endregion

    }
}