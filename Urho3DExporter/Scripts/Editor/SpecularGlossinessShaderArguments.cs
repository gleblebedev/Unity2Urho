using UnityEngine;

namespace Assets.Urho3DExporter.Scripts.Editor
{
    public class SpecularGlossinessShaderArguments: ShaderArguments
    {
        public Texture PBRSpecular { get; set; }
        public Texture Albedo { get; set; }
        public Texture DetailAlbedo { get; set; }
        public Color DiffuseColor { get; set; }
        public Color SpecularColor { get; set; }
        public SmoothnessTextureChannel SmoothnessTextureChannel { get; set; }
    }
}