using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class Subtract : GraphNode
    {
        public Subtract() : base("Subtract")
        {
            base.In.Add(X);
            base.In.Add(Y);
            base.Out.Add(Out);
        }

        public Subtract(GraphNode x, GraphNode y): this()
        {
            X.Connect(x);
            Y.Connect(y);
        }

        public Subtract(GraphOutPin x, GraphOutPin y): this()
        {
            X.TargetPin = x;
            Y.TargetPin = y;
        }

        public GraphInPin X { get; } = new GraphInPin("x", VariantType.None);

        public GraphInPin Y { get; } = new GraphInPin("y", VariantType.None);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.None);
    }
}