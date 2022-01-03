namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class Expire : GraphNode
    {
        public Expire() : base("Expire")
        {
            base.In.Add(Time);
            base.In.Add(Lifetime);
        }

        public Expire(GraphNode time, GraphNode lifetime): this()
        {
            Time.Connect(time);
            Lifetime.Connect(lifetime);
        }

        public Expire(GraphOutPin time, GraphOutPin lifetime): this()
        {
            Time.TargetPin = time;
            Lifetime.TargetPin = lifetime;
        }

        public GraphInPin Time { get; } = new GraphInPin("time", VariantType.Float);

        public GraphInPin Lifetime { get; } = new GraphInPin("lifetime", VariantType.Float);
    }
}