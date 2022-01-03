namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class Break : GraphNode
    {
        public Break() : base("Break")
        {
            base.In.Add(Vec);
            base.Out.Add(X);
            base.Out.Add(Y);
            base.Out.Add(Z);
        }

        public Break(GraphNode vec): this()
        {
            Vec.Connect(vec);
        }

        public Break(GraphOutPin vec): this()
        {
            Vec.TargetPin = vec;
        }

        public GraphInPin Vec { get; } = new GraphInPin("vec", VariantType.Vector3);

        public GraphOutPin X { get; } = new GraphOutPin("x", VariantType.Float);

        public GraphOutPin Y { get; } = new GraphOutPin("y", VariantType.Float);

        public GraphOutPin Z { get; } = new GraphOutPin("z", VariantType.Float);
    }
}