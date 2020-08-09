using System;
using System.IO;
using UnityEngine;
using UnityToCustomEngineExporter.Editor.StbSharpDxt;

namespace UnityToCustomEngineExporter.Editor
{
    public class DDS
    {
        public static void SaveAsRgbaDds(Texture2D texture, string fileName, bool hasAlpha = true)
        {
            using (var fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (var binaryWriter = new BinaryWriter(fileStream))
                {
                    bool compress = true;
                    if (compress)
                    {
                        WriteCompressedHeader(binaryWriter, texture.width, texture.height, texture.mipmapCount, false,
                            hasAlpha);
                        int width = texture.width;
                        int height = texture.height;
                        for (var mipIndex = 0; mipIndex < texture.mipmapCount; ++mipIndex)
                        {
                            WriteCompressed(binaryWriter, texture.GetPixels32(mipIndex), width, height, hasAlpha);
                            width = Math.Max(1, width / 2);
                            height = Math.Max(1, height / 2);
                        }
                    }
                    else
                    {
                        WriteHeader(binaryWriter, texture.width, texture.height, texture.mipmapCount, false);
                        for (var mipIndex = 0; mipIndex < texture.mipmapCount; ++mipIndex)
                            WriteAsIs(binaryWriter, texture.GetPixels32(mipIndex),
                                Math.Max(1, texture.width / (1 << mipIndex)));
                    }
                }
            }
        }

        private static void WriteCompressed(BinaryWriter binaryWriter, Color32[] getPixels32, int width, int height, bool hasAlpha)
        {
            //var data = StbSharpDxt.StbDxt.stb_compress_dxt(image, hasAlpha);
            var data = Compress(width, height, getPixels32, hasAlpha);
            binaryWriter.Write(data);
        }

        public static byte[] Compress(int width, int height, Color32[] pixels, bool hasAlpha)
        {
            return StbSharpDxt.StbDxt.CompressDxt(width, height, pixels, hasAlpha, CompressionMode.HighQuality);
        }

        public static void SaveAsRgbaDds(Cubemap texture, string fileName, bool convertToSRGB = false)
        {
            using (var fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (var binaryWriter = new BinaryWriter(fileStream))
                {
                    WriteHeader(binaryWriter, texture.width, texture.height, texture.mipmapCount, true);
                    var facesInOrder = new[]
                    {
                        CubemapFace.PositiveX,
                        CubemapFace.NegativeX,
                        CubemapFace.PositiveY,
                        CubemapFace.NegativeY,
                        CubemapFace.PositiveZ,
                        CubemapFace.NegativeZ
                    };
                    foreach (var cubemapFace in facesInOrder)
                        for (var mipIndex = 0; mipIndex < texture.mipmapCount; ++mipIndex)
                        {
                            var pixels = texture.GetPixels(cubemapFace, mipIndex);
                            var buf = new Color32[pixels.Length];
                            for (var index = 0; index < pixels.Length; index++) buf[index] = pixels[index];

                            if (convertToSRGB)
                                WriteLinearAsSRGB(binaryWriter, buf, Math.Max(1, texture.width / (1 << mipIndex)));
                            else
                                WriteAsIs(binaryWriter, buf, Math.Max(1, texture.width / (1 << mipIndex)));
                        }
                }
            }
        }

        private static void WriteCompressedHeader(BinaryWriter binaryWriter, int width, int height, int mipMapCount, bool cubemap, bool hasAlpha)
        {
            binaryWriter.Write((uint)0x20534444); // DDS magic
            binaryWriter.Write(124); // Size
            binaryWriter.Write(0x00001007 | 0x000A0000); // Flags
            binaryWriter.Write(height); // Height
            binaryWriter.Write(width); // Width
            if (hasAlpha)
            {
                binaryWriter.Write(height * width); // Pitch
            }
            else
            {
                binaryWriter.Write(height * width / 2); // Pitch
            }

            binaryWriter.Write(1); // Depth
            binaryWriter.Write(mipMapCount); // MipMapCount

            binaryWriter.Write(0); //Reserved1
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);

            // Pixel format
            binaryWriter.Write(32); //Size
            binaryWriter.Write(0x00000004); //compressed
            if (hasAlpha)
            {
                binaryWriter.Write(0x35545844); //DXT5
            }
            else
            {
                binaryWriter.Write(0x31545844); //DXT1
            }
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);

            // Caps
            if (cubemap)
            {
                binaryWriter.Write(0x00001000 | 0x00400008 | 0x00000008);
                binaryWriter.Write(0x00000600 | 0x00000a00 | 0x00001200 | 0x00002200 | 0x00004200 | 0x00008200);
            }
            else
            {
                binaryWriter.Write(0x00001000 | 0x00400008);
                binaryWriter.Write(0);
            }

            binaryWriter.Write(0); // Caps3
            binaryWriter.Write(0); // Caps4
            binaryWriter.Write(0); // Reserved
        }

        private static void WriteHeader(BinaryWriter binaryWriter, int width, int height, int mipMapCount, bool cubemap)
        {
            binaryWriter.Write((uint) 0x20534444); // DDS magic
            binaryWriter.Write(124); // Size
            binaryWriter.Write(0x00001007 | 0x00020000 | 0x00000008); // Flags
            binaryWriter.Write(height); // Height
            binaryWriter.Write(width); // Width
            binaryWriter.Write(width*4); // Pitch

            binaryWriter.Write(1); // Depth
            binaryWriter.Write(mipMapCount); // MipMapCount

            binaryWriter.Write(0); //Reserved1
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);

            // Pixel format
            binaryWriter.Write(32); //Size
            binaryWriter.Write(0x00000041); //RGBA
            binaryWriter.Write(0);
            binaryWriter.Write(32);
            binaryWriter.Write((uint) 0x000000ff);
            binaryWriter.Write((uint) 0x0000ff00);
            binaryWriter.Write((uint) 0x00ff0000);
            binaryWriter.Write(0xff000000);

            // Caps
            if (cubemap)
            {
                binaryWriter.Write(0x00001000 | 0x00400008 | 0x00000008);
                binaryWriter.Write(0x00000600 | 0x00000a00 | 0x00001200 | 0x00002200 | 0x00004200 | 0x00008200);
            }
            else
            {
                binaryWriter.Write(0x00001000 | 0x00400008);
                binaryWriter.Write(0);
            }

            binaryWriter.Write(0); // Caps3
            binaryWriter.Write(0); // Caps4
            binaryWriter.Write(0); // Reserved
        }


        private static Color32 LinearToSRGB(Color32 rgb)
        {
            Color RGB = rgb;
            var S1 = new Color(Mathf.Sqrt(RGB.r), Mathf.Sqrt(RGB.g), Mathf.Sqrt(RGB.b), RGB.a);
            var S2 = new Color(Mathf.Sqrt(S1.r), Mathf.Sqrt(S1.g), Mathf.Sqrt(S1.b), RGB.a);
            var S3 = new Color(Mathf.Sqrt(S2.r), Mathf.Sqrt(S2.g), Mathf.Sqrt(S2.b), RGB.a);
            var k1 = new Color(0.662002687f, 0.662002687f, 0.662002687f, 1);
            var k2 = new Color(0.684122060f, 0.684122060f, 0.684122060f, 1);
            var k3 = new Color(0.323583601f, 0.323583601f, 0.323583601f, 1);
            var k4 = new Color(0.0225411470f, 0.0225411470f, 0.0225411470f, 1);
            return k1 * S1 + k2 * S2 - k3 * S3 - k4 * RGB;
        }

        private static void WriteLinearAsSRGB(BinaryWriter binaryWriter, Color32[] getPixels, int textureWidth)
        {
            foreach (var c in getPixels)
            {
                var rgb = LinearToSRGB(c);
                binaryWriter.Write(rgb.r);
                binaryWriter.Write(rgb.g);
                binaryWriter.Write(rgb.b);
                binaryWriter.Write(c.a);
            }
        }

        private static void WriteAsIs(BinaryWriter binaryWriter, Color32[] getPixels32, int width)
        {
            var height = getPixels32.Length / width;
            for (var y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    var index = x + y * width;
                    binaryWriter.Write(getPixels32[index].r);
                    binaryWriter.Write(getPixels32[index].g);
                    binaryWriter.Write(getPixels32[index].b);
                    binaryWriter.Write(getPixels32[index].a);
                }
            }
        }
    }
}
