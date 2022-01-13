using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class Divide : GraphNode
    {
        public Divide() : base("Divide")
        {
            base.In.Add(X);
            base.In.Add(Y);
            base.Out.Add(Out);
        }

        public Divide(GraphNode x, GraphNode y): this()
        {
            X.Connect(x);
            Y.Connect(y);
        }

        public Divide(GraphOutPin x, GraphOutPin y): this()
        {
            X.TargetPin = x;
            Y.TargetPin = y;
        }

        public GraphInPin X { get; } = new GraphInPin("x", VariantType.None);

        public GraphInPin Y { get; } = new GraphInPin("y", VariantType.Float);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.None);
    }
}