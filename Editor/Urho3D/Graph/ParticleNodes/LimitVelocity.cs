namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class LimitVelocity : GraphNode
    {
        private readonly GraphNodeProperty<float> _dampen = new GraphNodeProperty<float>("Dampen");

        public LimitVelocity() : base("LimitVelocity")
        {
            base.In.Add(Velocity);
            base.In.Add(Limit);
            base.Out.Add(Out);
            base.Properties.Add(_dampen);
        }

        public LimitVelocity(GraphNode velocity, GraphNode limit) : this()
        {
            Velocity.Connect(velocity);
            Limit.Connect(limit);
        }

        public LimitVelocity(GraphOutPin velocity, GraphOutPin limit) : this()
        {
            Velocity.TargetPin = velocity;
            Limit.TargetPin = limit;
        }

        public float Dampen
        {
            get => _dampen.Value;
            set => _dampen.Value = value;
        }

        public GraphInPin Velocity { get; } = new GraphInPin("velocity");

        public GraphInPin Limit { get; } = new GraphInPin("limit");

        public GraphOutPin Out { get; } = new GraphOutPin("out");
    }
}