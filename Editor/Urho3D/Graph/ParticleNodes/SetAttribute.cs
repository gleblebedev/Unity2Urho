using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes
{
    public class SetAttribute : GraphNode
    {
        public SetAttribute(string name, VariantType type, GraphNode value) : base(GraphNodeType.SetAttribute)
        {
            Val = new GraphInPin("", type, value);
            Result = new GraphOutPin(name, type);
            In.Add(Val);
            Out.Add(Result);
        }

        public GraphInPin Val { get; }
        public GraphOutPin Result { get; }
    }
}