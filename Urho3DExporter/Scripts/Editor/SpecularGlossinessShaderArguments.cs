using UnityEngine;

namespace Assets.Urho3DExporter.Scripts.Editor
{
    public class SpecularGlossinessShaderArguments : ShaderArguments
    {
        public Texture PBRSpecular { get; set; }
        public Texture Diffuse { get; set; }
        public Texture DetailDiffuse { get; set; }
        public Color DiffuseColor { get; set; }
        public Color SpecularColor { get; set; }
        public SmoothnessTextureChannel SmoothnessTextureChannel { get; set; }
    }
}