using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class Destroy : GraphNode
    {
        public Destroy() : base("Destroy")
        {
            base.In.Add(Condition);
        }

        public Destroy(GraphNode condition): this()
        {
            Condition.Connect(condition);
        }

        public Destroy(GraphOutPin condition): this()
        {
            Condition.TargetPin = condition;
        }

        public GraphInPin Condition { get; } = new GraphInPin("condition", VariantType.Bool);
    }
}