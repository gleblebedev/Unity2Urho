using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class MetallicGlossinessShaderArguments : ShaderArguments
    {
        public Texture BaseColor { get; set; }
        public Texture MetallicGloss { get; set; }
        public Texture DetailBaseColor { get; set; }
        public Color BaseColorColor { get; set; } = Color.white;
        public float Glossiness { get; set; }
        public float Metallic { get; set; }
        public float MetallicScale { get; set; } = 1.0f;
        public float SmoothnessRemapMin { get; set; } = 0.0f;
        public float SmoothnessRemapMax { get; set; } = 1.0f;
        public SmoothnessTextureChannel SmoothnessTextureChannel { get; set; }

        public Texture Smoothness => SmoothnessTextureChannel == SmoothnessTextureChannel.MetallicOrSpecularAlpha
            ? MetallicGloss
            : BaseColor;
    }
}