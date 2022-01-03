namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class TimeStepScale : GraphNode
    {
        public TimeStepScale() : base("TimeStepScale")
        {
            base.In.Add(X);
            base.Out.Add(Out);
        }

        public TimeStepScale(GraphNode x): this()
        {
            X.Connect(x);
        }

        public TimeStepScale(GraphOutPin x): this()
        {
            X.TargetPin = x;
        }

        public GraphInPin X { get; } = new GraphInPin("x", VariantType.None);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.None);
    }
}