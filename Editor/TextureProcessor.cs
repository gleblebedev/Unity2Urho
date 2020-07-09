using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityToCustomEngineExporter.Editor
{
    public class TextureProcessor
    {
        public void ProcessAndSaveTexture(Texture sourceTexture, string shaderName, string fullOutputPath)
        {
            ProcessAndSaveTexture(sourceTexture, Shader.Find(shaderName), fullOutputPath);
        }

        public void ProcessAndSaveTexture(Texture sourceTexture, Shader shader, string fullOutputPath)
        {
            Material material = null;

            try
            {
                material = new Material(shader);
                ProcessAndSaveTexture(sourceTexture, material, fullOutputPath);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        public void ProcessAndSaveTexture(Texture sourceTexture, Material material, string fullOutputPath)
        {
            ProcessAndSaveTexture(sourceTexture, sourceTexture.width, sourceTexture.height, material, fullOutputPath);
        }

        public void ProcessAndSaveTexture(Texture sourceTexture, int width, int height, Material material,
            string fullOutputPath)
        {
            RenderTexture renderTex = null;
            Texture2D texture = null;
            var lastActiveRenderTexture = RenderTexture.active;

            try
            {
                var descriptor = new RenderTextureDescriptor
                {
                    width = width,
                    height = height,
                    colorFormat = RenderTextureFormat.ARGB32,
                    autoGenerateMips = false,
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
                texture = new Texture2D(width, height, TextureFormat.ARGB32, false /* mipmap */, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();

                SaveTexture(texture, fullOutputPath);
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

        private void SaveTexture(Texture2D texture, string fullOutputPath)
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
                    DDS.SaveAsRgbaDds(texture, fullOutputPath);
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