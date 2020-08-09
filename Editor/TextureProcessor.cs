using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor
{
    public class TextureProcessor
    {
        public void ProcessAndSaveTexture(Texture sourceTexture, string shaderName, string fullOutputPath, bool hasAlpha = true)
        {
            ProcessAndSaveTexture(sourceTexture, Shader.Find(shaderName), fullOutputPath, hasAlpha);
        }

        public void ProcessAndSaveTexture(Texture sourceTexture, Shader shader, string fullOutputPath, bool hasAlpha = true)
        {
            Material material = null;

            try
            {
                material = new Material(shader);
                ProcessAndSaveTexture(sourceTexture, material, fullOutputPath, hasAlpha);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        public void ProcessAndSaveTexture(Texture sourceTexture, Material material, string fullOutputPath, bool hasAlpha = true)
        {
            ProcessAndSaveTexture(sourceTexture, sourceTexture.width, sourceTexture.height, material, fullOutputPath, hasAlpha);
        }

        public void ProcessAndSaveTexture(Texture sourceTexture, int width, int height, Material material,
            string fullOutputPath, bool hasAlpha = true)
        {
            RenderTexture renderTex = null;
            Texture2D texture = null;
            var lastActiveRenderTexture = RenderTexture.active;

            try
            {
                var mips = sourceTexture.mipmapCount > 1;
                var descriptor = new RenderTextureDescriptor
                {
                    width = width,
                    height = height,
                    colorFormat = RenderTextureFormat.ARGB32,
                    autoGenerateMips = mips,
                    depthBufferBits = 16,
                    dimension = TextureDimension.Tex2D,
                    enableRandomWrite = false,
                    memoryless = RenderTextureMemoryless.None,
                    sRGB = false,
                    useMipMap = false,
                    volumeDepth = 1,
                    msaaSamples = 1
                };

                renderTex = RenderTexture.GetTemporary(descriptor);
                Graphics.Blit(sourceTexture, renderTex, material);

                RenderTexture.active = renderTex;
                texture = new Texture2D(width, height, TextureFormat.ARGB32, mips /* mipmap */, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, mips);
                texture.Apply();

                SaveTexture(texture, fullOutputPath, hasAlpha);
            }
            finally
            {
                RenderTexture.active = lastActiveRenderTexture;
                if (renderTex != null)
                    RenderTexture.ReleaseTemporary(renderTex);
                if (texture != null)
                    Object.DestroyImmediate(texture);
                if (material != null)
                    Object.DestroyImmediate(material);
            }
        }

        private void SaveTexture(Texture2D texture, string fullOutputPath, bool hasAlpha = true)
        {
            if (string.IsNullOrWhiteSpace(fullOutputPath))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));

            var ext = Path.GetExtension(fullOutputPath).ToLower();
            switch (ext)
            {
                case ".png":
                    var png = texture.EncodeToPNG();
                    WriteAllBytes(fullOutputPath, png);
                    break;
                case ".jpg":
                    var jpg = texture.EncodeToJPG();
                    WriteAllBytes(fullOutputPath, jpg);
                    break;
                case ".tga":
                    var tga = texture.EncodeToTGA();
                    WriteAllBytes(fullOutputPath, tga);
                    break;
                case ".exr":
                    var exr = texture.EncodeToEXR();
                    WriteAllBytes(fullOutputPath, exr);
                    break;
                case ".dds":
                    DDS.SaveAsRgbaDds(texture, fullOutputPath, hasAlpha);
                    break;
                default:
                    throw new NotImplementedException("Not implemented texture file type " + ext);
            }
        }

        private void WriteAllBytes(string fullOutputPath, byte[] buffer)
        {
            using (var fs = File.Open(fullOutputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                fs.Write(buffer, 0, buffer.Length);
            }
        }
    }
}