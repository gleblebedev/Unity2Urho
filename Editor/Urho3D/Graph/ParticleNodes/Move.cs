using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public class Move : GraphNode
    {
        public Move() : base(GraphNodeType.Move)
        {
            In.Add(Position);
            In.Add(Velocity);
            base.Out.Add(Result);
        }
        public Move(GraphNode pos, GraphNode vel) : this()
        {
            Position.Connect(pos);
            Velocity.Connect(vel);
        }
        public GraphInPin Position { get; } = new GraphInPin("position", VariantType.Vector3);
        public GraphInPin Velocity { get; } = new GraphInPin("velocity", VariantType.Vector3);
        public GraphOutPin Result { get; } = new GraphOutPin("newPosition", VariantType.Vector3);
    }
}