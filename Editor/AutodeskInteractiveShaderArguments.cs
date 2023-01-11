using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class AutodeskInteractiveShaderArguments : ShaderArguments
    {
        public Texture RoughnessMap { get; set; }
        public Texture MetallicMap { get; set; }
        public Color Color { get; set; }
        public float Glossiness { get; set; }
        public float Metallic { get; set; }
        public Texture Diffuse { get; set; }
    }
}