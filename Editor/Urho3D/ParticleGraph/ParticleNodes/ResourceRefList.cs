using System.Collections.Generic;
using System.Linq;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public struct ResourceRefList
    {
        public ResourceRefList(string type, params string[] path)
        {
            Type = type;
            Path = path.ToList();
        }
        public string Type { get; set; }
        public IList<string> Path { get; set; }
    }
}