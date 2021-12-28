using UnityEngine;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public class Sphere : GraphNode
    {
        private readonly GraphNodeProperty<float> _radius;
        private readonly GraphNodeProperty<float> _radiusThickness;

        private readonly GraphNodeProperty<EmitFrom> _from;
        private readonly GraphNodeProperty<Quaternion> _rotation;
        private readonly GraphNodeProperty<Vector3> _position;
        private readonly GraphNodeProperty<Vector3> _scale;

        public Sphere() : base(GraphNodeType.Sphere)
        {
            Position = new GraphOutPin("position", VariantType.Vector3);
            Velocity = new GraphOutPin("velocity", VariantType.Vector3);
            Properties.Add(_radius = new GraphNodeProperty<float>("Radius", 0.0f));
            Properties.Add(_radiusThickness = new GraphNodeProperty<float>("Radius Thickness", 0.0f));
            Properties.Add(_from = new GraphNodeProperty<EmitFrom>("From", EmitFrom.Base));
            Properties.Add(_rotation = GraphNodeProperty.Make("Rotation", Quaternion.identity));
            Properties.Add(_position = GraphNodeProperty.Make("Position", Vector3.zero));
            Properties.Add(_scale = GraphNodeProperty.Make("Scale", Vector3.one));

            Out.Add(Position);
            Out.Add(Velocity);
        }

        public GraphOutPin Position { get; }
        public GraphOutPin Velocity { get; }
        public float Radius
        {
            get => _radius.Value;
            set => _radius.Value = value;
        }

        public float RadiusThickness
        {
            get => _radiusThickness.Value;
            set => _radiusThickness.Value = value;
        }

        public Quaternion Rotation
        {
            get => _rotation.Value;
            set => _rotation.Value = value;
        }

        public Vector3 Translation
        {
            get => _position.Value;
            set => _position.Value = value;
        }

        public Vector3 Scale
        {
            get => _scale.Value;
            set => _scale.Value = value;
        }

        public EmitFrom From
        {
            get => _from.Value;
            set => _from.Value = value;
        }
    }
}