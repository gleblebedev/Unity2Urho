using System.Collections.Generic;
using UnityEngine;

namespace Assets.Urho3DExporter.Scripts.Editor
{
    public class TextureMetadata
    {
        public HashSet<TextureReferences> References = new HashSet<TextureReferences>();
        public Texture Texture { get; set; }
    }
}