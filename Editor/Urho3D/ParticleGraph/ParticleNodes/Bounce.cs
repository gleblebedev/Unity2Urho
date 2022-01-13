using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class Bounce : GraphNode
    {
        private readonly GraphNodeProperty<float> _dampen = new GraphNodeProperty<float>("Dampen");

        private readonly GraphNodeProperty<float> _bounceFactor = new GraphNodeProperty<float>("BounceFactor");

        public Bounce() : base("Bounce")
        {
            base.In.Add(Position);
            base.In.Add(Velocity);
            base.Out.Add(NewPosition);
            base.Out.Add(NewVelocity);
            base.Properties.Add(_dampen);
            base.Properties.Add(_bounceFactor);
        }

        public Bounce(GraphNode position, GraphNode velocity): this()
        {
            Position.Connect(position);
            Velocity.Connect(velocity);
        }

        public Bounce(GraphOutPin position, GraphOutPin velocity): this()
        {
            Position.TargetPin = position;
            Velocity.TargetPin = velocity;
        }

        public float Dampen {
            get => _dampen.Value;
            set => _dampen.Value = value;
        }

        public float BounceFactor {
            get => _bounceFactor.Value;
            set => _bounceFactor.Value = value;
        }

        public GraphInPin Position { get; } = new GraphInPin("position", VariantType.Vector3);

        public GraphInPin Velocity { get; } = new GraphInPin("velocity", VariantType.Vector3);

        public GraphOutPin NewPosition { get; } = new GraphOutPin("newPosition", VariantType.Vector3);

        public GraphOutPin NewVelocity { get; } = new GraphOutPin("newVelocity", VariantType.Vector3);
    }
}