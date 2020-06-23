using System;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor
{
    public class DDS
    {
        public static void SaveAsRgbaDds(Texture2D texture, string fileName)
        {
            using (var fileStream = File.Open(fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (var binaryWriter = new BinaryWriter(fileStream))
                {
                    WriteHeader(binaryWriter, texture.width, texture.height, texture.mipmapCount, false);
                    for (int mipIndex = 0; mipIndex < texture.mipmapCount; ++mipIndex)
                    {
                        WriteRgba(binaryWriter, texture.GetPixels32(mipIndex), Math.Max(1, texture.width / (1 << mipIndex)));
                    }
                }
            }
        }

        public static void SaveAsRgbaDds(Cubemap texture, string fileName)
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
                        CubemapFace.NegativeZ,
                    };
                    foreach (var cubemapFace in facesInOrder)
                    {
                        for (int mipIndex = 0; mipIndex < texture.mipmapCount; ++mipIndex)
                        {
                            var pixels = texture.GetPixels(cubemapFace, mipIndex);
                            var buf = new Color32[pixels.Length];
                            for (var index = 0; index < pixels.Length; index++)
                            {
                                buf[index] = pixels[index];
                            }
                            WriteRgba(binaryWriter, buf, Math.Max(1, texture.width / (1 << mipIndex)));
                        }
                    }
                }
            }
        }

        private static void WriteHeader(BinaryWriter binaryWriter, int width, int height, int mipMapCount, bool cubemap)
        {
            binaryWriter.Write((uint)0x20534444);
            binaryWriter.Write(124);
            binaryWriter.Write(0x00001007 | 0x00020000 | 0x00000008);
            binaryWriter.Write(height);
            binaryWriter.Write(width);
            binaryWriter.Write(width*4);
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

            binaryWriter.Write(7*4); //Size
            binaryWriter.Write(0x00000041); //RGBA
            binaryWriter.Write(0);
            binaryWriter.Write(32);
            binaryWriter.Write((uint)0x000000ff);
            binaryWriter.Write((uint)0x0000ff00);
            binaryWriter.Write((uint)0x00ff0000);
            binaryWriter.Write((uint)0xff000000);

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

        private static void WriteRgba(BinaryWriter binaryWriter, Color32[] getPixels, int textureWidth)
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