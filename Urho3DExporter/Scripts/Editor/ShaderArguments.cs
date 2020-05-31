using UnityEngine;

namespace Assets.Urho3DExporter.Scripts.Editor
{
    public class ShaderArguments
    {
        public bool AlphaTest { get; set; }

        public bool HasEmission { get; set; }

        public bool Transparent { get; set; }
        public Color EmissiveColor { get; set; } = Color.black;
        public Texture Occlusion { get; set; }
        public Texture Bump { get; set; }
        public Texture Detail { get; set; }
        public Texture DetailNormal { get; set; }
        public Texture Emission { get; set; }
        public Texture Parallax { get; set; }
    }
}