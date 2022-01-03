namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class Slerp : GraphNode
    {
        public Slerp() : base("Slerp")
        {
            base.In.Add(X);
            base.In.Add(Y);
            base.In.Add(T);
            base.Out.Add(Out);
        }

        public Slerp(GraphNode x, GraphNode y, GraphNode t): this()
        {
            X.Connect(x);
            Y.Connect(y);
            T.Connect(t);
        }

        public Slerp(GraphOutPin x, GraphOutPin y, GraphOutPin t): this()
        {
            X.TargetPin = x;
            Y.TargetPin = y;
            T.TargetPin = t;
        }

        public GraphInPin X { get; } = new GraphInPin("x", VariantType.Quaternion);

        public GraphInPin Y { get; } = new GraphInPin("y", VariantType.Quaternion);

        public GraphInPin T { get; } = new GraphInPin("t", VariantType.Float);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.Quaternion);
    }
}