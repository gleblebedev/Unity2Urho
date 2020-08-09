using UnityEditor;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class TextureOptions
    {
        public bool sRGBTexture;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public bool mipmapEnabled;
        public TextureImporterFormat? textureImporterFormat;

        public TextureOptions WithSRGB(bool srgb)
        {
            sRGBTexture = srgb;
            return this;
        }
    }
}