using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Postica.Common
{
    public class StackedImage : CodedImage
    {
        private byte[] _bytes;
        private CodedImage[] _images;
        private string _identifier;
        private (int width, int height) _size;

        internal protected override byte[] Bytes
        {
            get
            {
                if(_bytes == null)
                {
                    ComputeStackBytes();
                }
                return _bytes;
            }
        }

        public override (int width, int height) Size => _size;

        internal protected override bool IsPNGorJPEG => false;

        public override string Identifier => _identifier;

        public StackedImage(params CodedImage[] imagesInOrder)
        {
            _images = imagesInOrder;
            _identifier = "Stacked_" + string.Join("-", imagesInOrder.Select(i => i.Identifier).ToArray());
            _size = imagesInOrder[0].Size;
        }

        private void ComputeStackBytes()
        {
            _bytes = new byte[_size.width * _size.height * 4];
            for (int b = 0; b < _size.height; b++)
            {
                for (int i = 0; i < _images.Length; i++)
                {
                    _bytes[b] += (_images[i] as IByteArray).Bytes[b];
                    _bytes[b + 1] += (_images[i] as IByteArray).Bytes[b + 1];
                    _bytes[b + 2] += (_images[i] as IByteArray).Bytes[b + 2];
                    _bytes[b + 3] += (_images[i] as IByteArray).Bytes[b + 3];
                }
            }

        }
    }
}