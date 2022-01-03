namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class Multiply : GraphNode
    {
        public Multiply() : base("Multiply")
        {
            base.In.Add(X);
            base.In.Add(Y);
            base.Out.Add(Out);
        }

        public Multiply(GraphNode x, GraphNode y): this()
        {
            X.Connect(x);
            Y.Connect(y);
        }

        public Multiply(GraphOutPin x, GraphOutPin y): this()
        {
            X.TargetPin = x;
            Y.TargetPin = y;
        }

        public GraphInPin X { get; } = new GraphInPin("x", VariantType.None);

        public GraphInPin Y { get; } = new GraphInPin("y", VariantType.None);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.None);
    }
}