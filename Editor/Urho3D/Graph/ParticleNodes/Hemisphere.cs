using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class Hemisphere : GraphNode
    {
        private readonly GraphNodeProperty<float> _radius = new GraphNodeProperty<float>("Radius");

        private readonly GraphNodeProperty<float> _radiusThickness = new GraphNodeProperty<float>("Radius Thickness");

        private readonly GraphNodeProperty<Vector3> _translation = new GraphNodeProperty<Vector3>("Translation");

        private readonly GraphNodeProperty<Quaternion> _rotation = new GraphNodeProperty<Quaternion>("Rotation");

        private readonly GraphNodeProperty<Vector3> _scale = new GraphNodeProperty<Vector3>("Scale");

        private readonly GraphNodeProperty<EmitFrom> _from = new GraphNodeProperty<EmitFrom>("From");

        public Hemisphere() : base("Hemisphere")
        {
            base.Out.Add(Position);
            base.Out.Add(Velocity);
            base.Properties.Add(_radius);
            base.Properties.Add(_radiusThickness);
            base.Properties.Add(_translation);
            base.Properties.Add(_rotation);
            base.Properties.Add(_scale);
            base.Properties.Add(_from);
        }


        public float Radius {
            get => _radius.Value;
            set => _radius.Value = value;
        }

        public float RadiusThickness {
            get => _radiusThickness.Value;
            set => _radiusThickness.Value = value;
        }

        public Vector3 Translation {
            get => _translation.Value;
            set => _translation.Value = value;
        }

        public Quaternion Rotation {
            get => _rotation.Value;
            set => _rotation.Value = value;
        }

        public Vector3 Scale {
            get => _scale.Value;
            set => _scale.Value = value;
        }

        public EmitFrom From {
            get => _from.Value;
            set => _from.Value = value;
        }

        public GraphOutPin Position { get; } = new GraphOutPin("position", VariantType.Vector3);

        public GraphOutPin Velocity { get; } = new GraphOutPin("velocity", VariantType.Vector3);
    }
}