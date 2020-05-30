using UnityEngine;

namespace Assets.Urho3DExporter.Scripts.Editor
{
    public class MetallicRoughnessShaderArguments : ShaderArguments
    {
        public Texture Albedo { get; set; }
        public Texture MetallicGloss { get; set; }
        public Texture DetailAlbedo { get; set; }
    }
}