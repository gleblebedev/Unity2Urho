using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public partial class RenderBillboard : GraphNode
    {
        private readonly GraphNodeProperty<ResourceRef> _material = new GraphNodeProperty<ResourceRef>("Material");

        private readonly GraphNodeProperty<int> _rows = new GraphNodeProperty<int>("Rows");

        private readonly GraphNodeProperty<int> _columns = new GraphNodeProperty<int>("Columns");

        private readonly GraphNodeProperty<FaceCameraMode> _faceCameraMode = new GraphNodeProperty<FaceCameraMode>("Face Camera Mode");

        private readonly GraphNodeProperty<bool> _sortByDistance = new GraphNodeProperty<bool>("Sort By Distance");

        private readonly GraphNodeProperty<bool> _isWorldspace = new GraphNodeProperty<bool>("Is Worldspace");

        public RenderBillboard() : base("RenderBillboard")
        {
            base.In.Add(Pos);
            base.In.Add(Size);
            base.In.Add(Frame);
            base.In.Add(Color);
            base.In.Add(Rotation);
            base.In.Add(Direction);
            base.Properties.Add(_material);
            base.Properties.Add(_rows);
            base.Properties.Add(_columns);
            base.Properties.Add(_faceCameraMode);
            base.Properties.Add(_sortByDistance);
            base.Properties.Add(_isWorldspace);
        }

        public RenderBillboard(GraphNode pos, GraphNode size, GraphNode frame, GraphNode color, GraphNode rotation, GraphNode direction): this()
        {
            Pos.Connect(pos);
            Size.Connect(size);
            Frame.Connect(frame);
            Color.Connect(color);
            Rotation.Connect(rotation);
            Direction.Connect(direction);
        }

        public RenderBillboard(GraphOutPin pos, GraphOutPin size, GraphOutPin frame, GraphOutPin color, GraphOutPin rotation, GraphOutPin direction): this()
        {
            Pos.TargetPin = pos;
            Size.TargetPin = size;
            Frame.TargetPin = frame;
            Color.TargetPin = color;
            Rotation.TargetPin = rotation;
            Direction.TargetPin = direction;
        }

        public ResourceRef Material {
            get => _material.Value;
            set => _material.Value = value;
        }

        public int Rows {
            get => _rows.Value;
            set => _rows.Value = value;
        }

        public int Columns {
            get => _columns.Value;
            set => _columns.Value = value;
        }

        public FaceCameraMode FaceCameraMode {
            get => _faceCameraMode.Value;
            set => _faceCameraMode.Value = value;
        }

        public bool SortByDistance {
            get => _sortByDistance.Value;
            set => _sortByDistance.Value = value;
        }

        public bool IsWorldspace {
            get => _isWorldspace.Value;
            set => _isWorldspace.Value = value;
        }

        public GraphInPin Pos { get; } = new GraphInPin("pos", VariantType.Vector3);

        public GraphInPin Size { get; } = new GraphInPin("size", VariantType.Vector2);

        public GraphInPin Frame { get; } = new GraphInPin("frame", VariantType.Float);

        public GraphInPin Color { get; } = new GraphInPin("color", VariantType.Color);

        public GraphInPin Rotation { get; } = new GraphInPin("rotation", VariantType.Float);

        public GraphInPin Direction { get; } = new GraphInPin("direction", VariantType.Vector3);
    }
}