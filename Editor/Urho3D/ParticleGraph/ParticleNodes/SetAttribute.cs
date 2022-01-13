using System.Linq;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public class SetAttribute : GraphNode
    {
        public SetAttribute(string name, VariantType type, GraphNode value) : this(name, type, value.Out.FirstOrDefault())
        {
        }
        public SetAttribute(string name, VariantType type, GraphOutPin value) : base(GraphNodeType.SetAttribute)
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