using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public class GetAttribute : GraphNode
    {
        public GetAttribute(string name, VariantType type) : base(GraphNodeType.GetAttribute)
        {
            Result = new GraphOutPin(name, type);
            Out.Add(Result);
        }

        public GraphOutPin Result { get; }
    }
}