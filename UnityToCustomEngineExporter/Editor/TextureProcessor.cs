using System;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor
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
                GameObject.DestroyImmediate(material);
            }
        }

        public void ProcessAndSaveTexture(Texture sourceTexture, Material material, string fullOutputPath)
        {
            RenderTexture renderTex = null;
            Texture2D texture = null;
            var lastActiveRenderTexture = RenderTexture.active;

            try
            {
                RenderTextureDescriptor descriptor = new RenderTextureDescriptor()
                {
                    width = sourceTexture.width,
                    height = sourceTexture.height,
                    colorFormat = RenderTextureFormat.ARGB32,
                    autoGenerateMips = false,
                    depthBufferBits = 16,
                    dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
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
                int width = renderTex.width;
                int height = renderTex.height;
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
                    GameObject.DestroyImmediate(texture);
                if (material != null)
                    GameObject.DestroyImmediate(material);
            }
        }

        private void SaveTexture(Texture2D texture, string fullOutputPath)
        {
            var ext = Path.GetExtension(fullOutputPath).ToLower();
            switch (ext)
            {
                case ".png":
                    var png = texture.EncodeToPNG();
                    File.WriteAllBytes(fullOutputPath, png);
                    break;
                case ".jpg":
                    var jpg = texture.EncodeToJPG();
                    File.WriteAllBytes(fullOutputPath, jpg);
                    break;
                case ".tga":
                    var tga = texture.EncodeToTGA();
                    File.WriteAllBytes(fullOutputPath, tga);
                    break;
                case ".exr":
                    var exr = texture.EncodeToEXR();
                    File.WriteAllBytes(fullOutputPath, exr);
                    break;
                case ".dds":
                    DDS.SaveAsRgbaDds(texture, fullOutputPath);
                    break;
                default:
                    throw new NotImplementedException("Not implemented texture file type "+ext);
            }
        }
    }
}

