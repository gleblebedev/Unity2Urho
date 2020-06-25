using System.Collections.Generic;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor
{
    public class TextureMetadata
    {
        public HashSet<TextureReference> References = new HashSet<TextureReference>();
        public Texture Texture { get; set; }
    }
}