namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class Lerp : GraphNode
    {
        public Lerp() : base("Lerp")
        {
            base.In.Add(X);
            base.In.Add(Y);
            base.In.Add(T);
            base.Out.Add(Out);
        }

        public Lerp(GraphNode x, GraphNode y, GraphNode t): this()
        {
            X.Connect(x);
            Y.Connect(y);
            T.Connect(t);
        }

        public Lerp(GraphOutPin x, GraphOutPin y, GraphOutPin t): this()
        {
            X.TargetPin = x;
            Y.TargetPin = y;
            T.TargetPin = t;
        }

        public GraphInPin X { get; } = new GraphInPin("x", VariantType.None);

        public GraphInPin Y { get; } = new GraphInPin("y", VariantType.None);

        public GraphInPin T { get; } = new GraphInPin("t", VariantType.Float);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.None);
    }
}