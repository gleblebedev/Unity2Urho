using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class Negate : GraphNode
    {
        public Negate() : base("Negate")
        {
            base.In.Add(X);
            base.Out.Add(Out);
        }

        public Negate(GraphNode x): this()
        {
            X.Connect(x);
        }

        public Negate(GraphOutPin x): this()
        {
            X.TargetPin = x;
        }

        public GraphInPin X { get; } = new GraphInPin("x", VariantType.None);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.None);
    }
}