using UnityEngine;

using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph.ParticleNodes
{
    public partial class Box : GraphNode
    {
        private readonly GraphNodeProperty<Vector3> _boxThickness = new GraphNodeProperty<Vector3>("Box Thickness");

        private readonly GraphNodeProperty<Vector3> _translation = new GraphNodeProperty<Vector3>("Translation");

        private readonly GraphNodeProperty<Quaternion> _rotation = new GraphNodeProperty<Quaternion>("Rotation");

        private readonly GraphNodeProperty<Vector3> _scale = new GraphNodeProperty<Vector3>("Scale");

        private readonly GraphNodeProperty<EmitFrom> _from = new GraphNodeProperty<EmitFrom>("From");

        public Box() : base("Box")
        {
            base.Out.Add(Position);
            base.Out.Add(Velocity);
            base.Properties.Add(_boxThickness);
            base.Properties.Add(_translation);
            base.Properties.Add(_rotation);
            base.Properties.Add(_scale);
            base.Properties.Add(_from);
        }


        public Vector3 BoxThickness {
            get => _boxThickness.Value;
            set => _boxThickness.Value = value;
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