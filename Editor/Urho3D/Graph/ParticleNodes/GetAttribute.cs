using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes
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