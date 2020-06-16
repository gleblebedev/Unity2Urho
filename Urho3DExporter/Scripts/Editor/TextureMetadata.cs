using System.Collections.Generic;
using UnityEngine;

namespace Assets.Urho3DExporter.Scripts.Editor
{
    public class TextureMetadata
    {
        public HashSet<TextureReference> References = new HashSet<TextureReference>();
        public Texture Texture { get; set; }
    }
}