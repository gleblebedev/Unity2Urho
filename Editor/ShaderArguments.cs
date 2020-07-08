using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class ShaderArguments
    {
        public string Shader { get; set; }
        public bool AlphaTest { get; set; }
        public bool HasEmission { get; set; }
        public bool Transparent { get; set; }
        public float Cutoff { get; set; } = 0.5f;
        public float BumpScale { get; set; } = 1.0f;
        public Color EmissiveColor { get; set; } = Color.black;
        public Texture Occlusion { get; set; }
        public float OcclusionStrength { get; set; } = 1.0f;
        public Texture Bump { get; set; }
        public Texture Detail { get; set; }
        public Texture DetailNormal { get; set; }
        public Texture Emission { get; set; }
        public Texture Parallax { get; set; }
        public Vector2 MainTextureOffset { get; set; }
        public Vector2 MainTextureScale { get; set; } = Vector2.one;
    }
}