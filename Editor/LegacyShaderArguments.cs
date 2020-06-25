using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class LegacyShaderArguments : ShaderArguments
    {
        private static readonly Color Zero = new Color(0, 0, 0, 0);
        public Texture Diffuse { get; set; }
        public Color DiffColor { get; set; } = Color.white;
        public Color SpecColor { get; set; } = Zero;
        public Texture Specular { get; set; }
    }
}