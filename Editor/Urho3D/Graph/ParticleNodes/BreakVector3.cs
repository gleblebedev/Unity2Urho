namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class BreakVector3 : GraphNode
    {
        public BreakVector3() : base("BreakVector3")
        {
            base.In.Add(Vec);
            base.Out.Add(X);
            base.Out.Add(Y);
            base.Out.Add(Z);
        }

        public BreakVector3(GraphNode vec) : this()
        {
            Vec.Connect(vec);
        }

        public BreakVector3(GraphOutPin vec) : this()
        {
            Vec.TargetPin = vec;
        }

        public GraphInPin Vec { get; } = new GraphInPin("vec", VariantType.Vector3);

        public GraphOutPin X { get; } = new GraphOutPin("x", VariantType.Float);

        public GraphOutPin Y { get; } = new GraphOutPin("y", VariantType.Float);

        public GraphOutPin Z { get; } = new GraphOutPin("z", VariantType.Float);
    }
}