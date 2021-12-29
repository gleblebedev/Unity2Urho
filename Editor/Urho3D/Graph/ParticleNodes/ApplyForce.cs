namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class ApplyForce : GraphNode
    {
        public ApplyForce() : base("ApplyForce")
        {
            base.In.Add(Velocity);
            base.In.Add(Force);
            base.Out.Add(Out);
        }

        public ApplyForce(GraphNode velocity, GraphNode force) : this()
        {
            Velocity.Connect(velocity);
            Force.Connect(force);
        }

        public ApplyForce(GraphOutPin velocity, GraphOutPin force) : this()
        {
            Velocity.TargetPin = velocity;
            Force.TargetPin = force;
        }

        public GraphInPin Velocity { get; } = new GraphInPin("velocity", VariantType.Vector3);

        public GraphInPin Force { get; } = new GraphInPin("force", VariantType.Vector3);

        public GraphOutPin Out { get; } = new GraphOutPin("out", VariantType.Vector3);
    }
}