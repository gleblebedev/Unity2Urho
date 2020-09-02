using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Tests
{
    [TestFixture]
    public class StbDxtTests
    {
        public Color32[] GenArray(int width, int height, Color32 color)
        {
            var res = new Color32[width * height];
            for (var index = 0; index < res.Length; index++)
            {
                res[index] = color;
            }

            return res;
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 1)]
        [TestCase(4, 1)]
        [TestCase(5, 1)]
        [TestCase(6, 1)]
        [TestCase(7, 1)]
        [TestCase(8, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 2)]
        [TestCase(3, 2)]
        [TestCase(4, 2)]
        [TestCase(5, 2)]
        [TestCase(6, 2)]
        [TestCase(7, 2)]
        [TestCase(8, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 3)]
        [TestCase(3, 3)]
        [TestCase(4, 3)]
        [TestCase(5, 3)]
        [TestCase(6, 3)]
        [TestCase(7, 3)]
        [TestCase(8, 3)]
        [TestCase(1, 4)]
        [TestCase(2, 4)]
        [TestCase(3, 4)]
        [TestCase(4, 4)]
        [TestCase(5, 4)]
        [TestCase(6, 4)]
        [TestCase(7, 4)]
        [TestCase(8, 4)]
        [TestCase(1, 5)]
        [TestCase(2, 5)]
        [TestCase(3, 5)]
        [TestCase(4, 5)]
        [TestCase(5, 5)]
        [TestCase(6, 5)]
        [TestCase(7, 5)]
        [TestCase(8, 5)]
        [TestCase(1, 6)]
        [TestCase(2, 6)]
        [TestCase(3, 6)]
        [TestCase(4, 6)]
        [TestCase(5, 6)]
        [TestCase(6, 6)]
        [TestCase(7, 6)]
        [TestCase(8, 6)]
        [TestCase(1, 7)]
        [TestCase(2, 7)]
        [TestCase(3, 7)]
        [TestCase(4, 7)]
        [TestCase(5, 7)]
        [TestCase(6, 7)]
        [TestCase(7, 7)]
        [TestCase(8, 7)]
        [TestCase(1, 8)]
        [TestCase(2, 8)]
        [TestCase(3, 8)]
        [TestCase(4, 8)]
        [TestCase(5, 8)]
        [TestCase(6, 8)]
        [TestCase(7, 8)]
        [TestCase(8, 8)]
        public void CompressSingleColorTexture_MatchRepeatedBlocks(int width, int height)
        {
            var res = DDS.Compress(width, height, GenArray(width, height, new Color32(0xB8, 0xD0, 0xC0, 0xFF)), false);
            var block = DDS.Compress(4, 4, GenArray(4, 4, new Color32(0xB8, 0xD0, 0xC0, 0xFF)), false);
            Assert.AreEqual(res.Length, block.Length*((width+3)/4) * ((height + 3) / 4));
            for (int i = 0; i < res.Length; i += block.Length)
            {
                for (int j = 0; j < block.Length; ++j)
                {
                    Assert.AreEqual(res[i + j], block[j]);
                }
            }
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 1)]
        [TestCase(4, 1)]
        [TestCase(5, 1)]
        [TestCase(6, 1)]
        [TestCase(7, 1)]
        [TestCase(8, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 2)]
        [TestCase(3, 2)]
        [TestCase(4, 2)]
        [TestCase(5, 2)]
        [TestCase(6, 2)]
        [TestCase(7, 2)]
        [TestCase(8, 2)]
        [TestCase(1, 3)]
        [TestCase(2, 3)]
        [TestCase(3, 3)]
        [TestCase(4, 3)]
        [TestCase(5, 3)]
        [TestCase(6, 3)]
        [TestCase(7, 3)]
        [TestCase(8, 3)]
        [TestCase(1, 4)]
        [TestCase(2, 4)]
        [TestCase(3, 4)]
        [TestCase(4, 4)]
        [TestCase(5, 4)]
        [TestCase(6, 4)]
        [TestCase(7, 4)]
        [TestCase(8, 4)]
        [TestCase(1, 5)]
        [TestCase(2, 5)]
        [TestCase(3, 5)]
        [TestCase(4, 5)]
        [TestCase(5, 5)]
        [TestCase(6, 5)]
        [TestCase(7, 5)]
        [TestCase(8, 5)]
        [TestCase(1, 6)]
        [TestCase(2, 6)]
        [TestCase(3, 6)]
        [TestCase(4, 6)]
        [TestCase(5, 6)]
        [TestCase(6, 6)]
        [TestCase(7, 6)]
        [TestCase(8, 6)]
        [TestCase(1, 7)]
        [TestCase(2, 7)]
        [TestCase(3, 7)]
        [TestCase(4, 7)]
        [TestCase(5, 7)]
        [TestCase(6, 7)]
        [TestCase(7, 7)]
        [TestCase(8, 7)]
        [TestCase(1, 8)]
        [TestCase(2, 8)]
        [TestCase(3, 8)]
        [TestCase(4, 8)]
        [TestCase(5, 8)]
        [TestCase(6, 8)]
        [TestCase(7, 8)]
        [TestCase(8, 8)]
        public void CompressSingleColorTextureWithAlpha_MatchRepeatedBlocks(int width, int height)
        {
            var res = DDS.Compress(width, height, GenArray(width, height, new Color32(0xB8, 0xD0, 0xC0, 0xFF)), true);
            var block = DDS.Compress(4, 4, GenArray(4, 4, new Color32(0xB8, 0xD0, 0xC0, 0xFF)), true);
            Assert.AreEqual(res.Length, block.Length * ((width + 3) / 4) * ((height + 3) / 4));
            for (int i = 0; i < res.Length; i += block.Length)
            {
                for (int j = 0; j < block.Length; ++j)
                {
                    Assert.AreEqual(res[i + j], block[j]);
                }
            }
        }

        [Test]
        public void CompressNotPowTexture()
        {
            var width = 7;
            var height = 5;
            var sourcePixels = new Color32[height*width];
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    Color32 c;
                    if (x == width - 1 && y == height - 1)
                    {
                        c = new Color32(0xD8, 0xF8, 0x08, 0xD0);
                    }
                    else if (x == 0)
                    {
                        c = new Color32(0x88, 0xE0, 0x70, 0x30);
                    }
                    else if (y == 0)
                    {
                        c = new Color32(0x40, 0x48, 0x70, 0xF8);
                    }
                    else if (x == width-1)
                    {
                        c = new Color32(0x78, 0xF8, 0x00, 0xA0);
                    }
                    else if (y == height-1)
                    {
                        c = new Color32(0xF8, 0xA8, 0x50, 0xD8);
                    }
                    else
                    {
                        c = new Color32(0xD0, 0xC0, 0x88, 0xB8);
                    }
                    sourcePixels[y*width+x] = c;
                }
            }

            var compressed = DDS.Compress(width, height, sourcePixels, false, false);
            var compressedWithAlpha = DDS.Compress(width, height, sourcePixels, true, false);

            //Debug.Log(string.Join(", ", compressed.Select(_=>string.Format("0x{0:X2}", _))));
            //Debug.Log(string.Join(", ", compressedWithAlpha.Select(_ => string.Format("0x{0:X2}", _))));

            Assert.AreEqual(new byte[] { 0x6E, 0xF5, 0x0F, 0x87, 0x01, 0xA9, 0xA9, 0xA9, 0xCF, 0xDD, 0xC0, 0x87, 0xF0, 0x50, 0x50, 0x50, 0xEE, 0x8E, 0x4E, 0x42, 0x54, 0x54, 0x54, 0x54, 0x8F, 0x4A, 0xCB, 0x29, 0xAA, 0xAA, 0xAA, 0xAA }, compressed);
            Assert.AreEqual(new byte[] { 0xD8, 0x30, 0x01, 0x10, 0x49, 0x91, 0x14, 0x49, 0x6E, 0xF5, 0x0F, 0x87, 0x01, 0xA9, 0xA9, 0xA9, 0xD8, 0xA0, 0x80, 0xD4, 0x26, 0x6D, 0xD2, 0x26, 0xCF, 0xDD, 0xC0, 0x87, 0xF0, 0x50, 0x50, 0x50, 0xF8, 0x30, 0x01, 0x10, 0x00, 0x01, 0x10, 0x00, 0xEE, 0x8E, 0x4E, 0x42, 0x54, 0x54, 0x54, 0x54, 0xF8, 0xF8, 0x49, 0x92, 0x24, 0x49, 0x92, 0x24, 0x8F, 0x4A, 0xCB, 0x29, 0xAA, 0xAA, 0xAA, 0xAA }, compressedWithAlpha);

            compressed = DDS.Compress(width, height, sourcePixels, false, true);
            compressedWithAlpha = DDS.Compress(width, height, sourcePixels, true, true);

            Assert.AreEqual(new byte[] { 0x2D, 0xED, 0xCF, 0x8E, 0x01, 0xA9, 0xA9, 0xA9, 0xAE, 0xDD, 0xA0, 0x7F, 0xF0, 0x50, 0x50, 0x50, 0xCD, 0x86, 0x2D, 0x3A, 0x54, 0x54, 0x54, 0x54, 0x8F, 0x4A, 0xCB, 0x29, 0xAA, 0xAA, 0xAA, 0xAA }, compressed);
            Assert.AreEqual(new byte[] { 0xD8, 0x30, 0x01, 0x10, 0x49, 0x91, 0x14, 0x49, 0x2D, 0xED, 0xCF, 0x8E, 0x01, 0xA9, 0xA9, 0xA9, 0xD8, 0xA0, 0x80, 0xD4, 0x26, 0x6D, 0xD2, 0x26, 0xAE, 0xDD, 0xA0, 0x7F, 0xF0, 0x50, 0x50, 0x50, 0xF8, 0x30, 0x01, 0x10, 0x00, 0x01, 0x10, 0x00, 0xCD, 0x86, 0x2D, 0x3A, 0x54, 0x54, 0x54, 0x54, 0xF8, 0xF8, 0x49, 0x92, 0x24, 0x49, 0x92, 0x24, 0x8F, 0x4A, 0xCB, 0x29, 0xAA, 0xAA, 0xAA, 0xAA }, compressedWithAlpha);
        }
    }
}