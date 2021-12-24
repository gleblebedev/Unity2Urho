using UnityEngine;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes
{
    public class Cone : GraphNode
    {
        private readonly GraphNodeProperty<float> _radius;
        private readonly GraphNodeProperty<float> _angle;
        private readonly GraphNodeProperty<EmitFrom> _from;
        private readonly GraphNodeProperty<Quaternion> _rotation;
        private readonly GraphNodeProperty<Vector3> _translation;
        private readonly GraphNodeProperty<float> _radiusThickness;
        private readonly GraphNodeProperty<float> _length;

        public Cone() : base(GraphNodeType.Cone)
        {
            Position = new GraphOutPin("position", VariantType.Vector3);
            Velocity = new GraphOutPin("velocity", VariantType.Vector3);
            Properties.Add(_radius = new GraphNodeProperty<float>("Radius", 0.0f));
            Properties.Add(_radiusThickness = new GraphNodeProperty<float>("Radius Thickness", 0.0f));
            Properties.Add(_angle = new GraphNodeProperty<float>("Angle", 45.0f));
            Properties.Add(_length = new GraphNodeProperty<float>("Length", 1.0f));
            Properties.Add(_from = new GraphNodeProperty<EmitFrom>("From", EmitFrom.Base));
            Properties.Add(_rotation = GraphNodeProperty.Make("Rotation", Quaternion.identity));
            Properties.Add(_translation = GraphNodeProperty.Make("Translation", Vector3.zero));
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

        public float Length
        {
            get => _length.Value;
            set => _length.Value = value;
        }

        public float RadiusThickness
        {
            get => _radiusThickness.Value;
            set => _radiusThickness.Value = value;
        }
        public float Angle
        {
            get => _angle.Value;
            set => _angle.Value = value;
        }

        public Quaternion Rotation
        {
            get => _rotation.Value;
            set => _rotation.Value = value;
        }

        public Vector3 Translation
        {
            get => _translation.Value;
            set => _translation.Value = value;
        }

        public EmitFrom From
        {
            get => _from.Value;
            set => _from.Value = value;
        }
    }
}