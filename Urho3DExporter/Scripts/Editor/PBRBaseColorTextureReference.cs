using UnityEngine;

namespace Assets.Scripts.UnityToCustomEngineExporter.Editor
{
    public class PBRBaseColorTextureReference :TextureReference
    {
        public PBRBaseColorTextureReference():base(TextureSemantic.PBRBaseColor)
        {
        }
    }
}