using System.Collections.Generic;
using UnityEngine;

namespace Assets.Urho3DExporter.Scripts.Editor
{
    public class TextureMetadata
    {
        public Texture Texture { get; set; }
        public HashSet<TextureReferences> References = new HashSet<TextureReferences>();
    }
}