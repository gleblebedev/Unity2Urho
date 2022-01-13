using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class Move : GraphNode
    {
        public Move() : base("Move")
        {
            base.In.Add(Position);
            base.In.Add(Velocity);
            base.Out.Add(NewPosition);
        }

        public Move(GraphNode position, GraphNode velocity): this()
        {
            Position.Connect(position);
            Velocity.Connect(velocity);
        }

        public Move(GraphOutPin position, GraphOutPin velocity): this()
        {
            Position.TargetPin = position;
            Velocity.TargetPin = velocity;
        }

        public GraphInPin Position { get; } = new GraphInPin("position", VariantType.Vector3);

        public GraphInPin Velocity { get; } = new GraphInPin("velocity", VariantType.Vector3);

        public GraphOutPin NewPosition { get; } = new GraphOutPin("newPosition", VariantType.Vector3);
    }
}