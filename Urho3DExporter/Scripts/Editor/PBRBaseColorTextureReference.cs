using UnityEngine;

namespace Assets.Urho3DExporter.Scripts.Editor
{
    public class PBRBaseColorTextureReference :TextureReference
    {
        public PBRBaseColorTextureReference(Texture opacityMask):base(TextureSemantic.PBRBaseColor)
        {
            OpacityMask = opacityMask;
        }

        public Texture OpacityMask { get; set; }
    }
}