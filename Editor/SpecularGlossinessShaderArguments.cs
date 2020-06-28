using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class SpecularGlossinessShaderArguments : ShaderArguments
    {
        public SpecularGlossinessShaderArguments()
        {
            PBRSpecular = new TextureOrColor(null, Color.black);
        }

        public TextureOrColor PBRSpecular { get; set; }
        public Texture Diffuse { get; set; }
        public Texture DetailDiffuse { get; set; }
        public Color DiffuseColor { get; set; }
        public SmoothnessTextureChannel SmoothnessTextureChannel { get; set; }
        public float GlossinessTextureScale { get; set; } = 1;

        public TextureOrColor Smoothness => SmoothnessTextureChannel == SmoothnessTextureChannel.MetallicOrSpecularAlpha
            ? PBRSpecular
            : new TextureOrColor(Diffuse, DiffuseColor);

        public float Glossiness { get; set; }
    }
}