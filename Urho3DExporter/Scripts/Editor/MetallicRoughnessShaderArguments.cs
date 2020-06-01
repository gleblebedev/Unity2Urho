using UnityEngine;

namespace Assets.Urho3DExporter.Scripts.Editor
{
    public class MetallicRoughnessShaderArguments : ShaderArguments
    {
        public Texture BaseColor { get; set; }
        public Texture MetallicGloss { get; set; }
        public Texture DetailBaseColor { get; set; }
        public Color BaseColorColor { get; set; } = Color.white;
        public float Glossiness { get; set; }
        public float Metallic { get; set; }
        public SmoothnessTextureChannel SmoothnessTextureChannel { get; set; }
    }
}