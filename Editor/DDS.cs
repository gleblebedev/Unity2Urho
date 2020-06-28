using System;
using System.IO;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class DDS
    {
        public static void SaveAsRgbaDds(Texture2D texture, string fileName, bool convertToSRGB = false)
        {
            using (var fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (var binaryWriter = new BinaryWriter(fileStream))
                {
                    WriteHeader(binaryWriter, texture.width, texture.height, texture.mipmapCount, false);
                    for (var mipIndex = 0; mipIndex < texture.mipmapCount; ++mipIndex)
                        WriteAsIs(binaryWriter, texture.GetPixels32(mipIndex),
                            Math.Max(1, texture.width / (1 << mipIndex)));
                }
            }
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

        private static void WriteHeader(BinaryWriter binaryWriter, int width, int height, int mipMapCount, bool cubemap)
        {
            binaryWriter.Write((uint) 0x20534444);
            binaryWriter.Write(124);
            binaryWriter.Write(0x00001007 | 0x00020000 | 0x00000008);
            binaryWriter.Write(height);
            binaryWriter.Write(width);
            binaryWriter.Write(width * 4);
            binaryWriter.Write(1);
            binaryWriter.Write(mipMapCount);

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
            binaryWriter.Write(0);

            binaryWriter.Write(7 * 4); //Size
            binaryWriter.Write(0x00000041); //RGBA
            binaryWriter.Write(0);
            binaryWriter.Write(32);
            binaryWriter.Write((uint) 0x000000ff);
            binaryWriter.Write((uint) 0x0000ff00);
            binaryWriter.Write((uint) 0x00ff0000);
            binaryWriter.Write(0xff000000);

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

            binaryWriter.Write(0);
            binaryWriter.Write(0);
            binaryWriter.Write(0);
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

        private static void WriteAsIs(BinaryWriter binaryWriter, Color32[] getPixels, int textureWidth)
        {
            foreach (var c in getPixels)
            {
                binaryWriter.Write(c.r);
                binaryWriter.Write(c.g);
                binaryWriter.Write(c.b);
                binaryWriter.Write(c.a);
            }
        }
    }
}