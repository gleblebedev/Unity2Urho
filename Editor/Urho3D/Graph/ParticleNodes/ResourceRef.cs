using System.Collections;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public struct ResourceRef
    {
        public ResourceRef(string type, string path)
        {
            Type = type;
            Path = path;
        }
        public string Type { get; set; }
        public string Path { get; set; }
    }
}