namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class MakeVec2 : GraphNode
    {
        public MakeVec2() : base("MakeVec2")
        {
            base.In.Add(X);
            base.In.Add(Y);
            base.Out.Add(Out);
        }

        public MakeVec2(GraphNode x, GraphNode y) : this()
        {
            X.Connect(x);
            Y.Connect(y);
        }

        public MakeVec2(GraphOutPin x, GraphOutPin y) : this()
        {
            X.TargetPin = x;
            Y.TargetPin = y;
        }

        public GraphInPin X { get; } = new GraphInPin("x", VariantType.Float);

        public GraphInPin Y { get; } = new GraphInPin("y", VariantType.Float);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.Vector2);
    }
}