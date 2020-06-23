using Assets.Scripts.UnityToCustomEngineExporter.Editor;
using UnityEngine;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor
{
    public class SpecularGlossinessShaderArguments : ShaderArguments
    {
        public Texture PBRSpecular { get; set; }
        public Texture Diffuse { get; set; }
        public Texture DetailDiffuse { get; set; }
        public Color DiffuseColor { get; set; }
        public Color SpecularColor { get; set; }
        public SmoothnessTextureChannel SmoothnessTextureChannel { get; set; }
        public float GlossinessTextureScale { get; set; } = 1;

        public Texture Smoothness => (SmoothnessTextureChannel == SmoothnessTextureChannel.MetallicOrSpecularAlpha)
            ? PBRSpecular
            : Diffuse;

        public float Glossiness { get; set; }
    }
}