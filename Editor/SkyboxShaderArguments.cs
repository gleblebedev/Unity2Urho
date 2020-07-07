using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class SkyboxShaderArguments : ShaderArguments
    {
        public Texture Skybox { get; set; }
        public Texture FrontTex { get; set; }
        public Texture BackTex { get; set; }
        public Texture LeftTex { get; set; }
        public Texture RightTex { get; set; }
        public Texture UpTex { get; set; }
        public Texture DownTex { get; set; }
    }
}