using System.Globalization;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph.ParticleNodes
{
    public class RenderBillboard : GraphNode
    {
        private GraphNodeProperty<ResourceRef> _material = new GraphNodeProperty<ResourceRef>("Material", new ResourceRef("Material", ""));
        private GraphNodeProperty<int> _columns = new GraphNodeProperty<int>("Columns", 1);
        private GraphNodeProperty<int> _rows = new GraphNodeProperty<int>("Rows", 1);

        public RenderBillboard() : base(GraphNodeType.RenderBillboard)
        {
            Properties.Add(_material);
            Properties.Add(_columns);
            Properties.Add(_rows);
            In.Add(Pos);
            In.Add(Size);
            In.Add(Frame);
            In.Add(Color);
            In.Add(Rotation);
        }

        public GraphInPin Pos { get; } = new GraphInPin("pos", VariantType.Vector3) { Value = "0 0 0" };
        public GraphInPin Size { get; } = new GraphInPin("size", VariantType.Vector2) { Value = "1 1" };
        public GraphInPin Frame { get; } = new GraphInPin("frame", VariantType.Float) { Value = "0" };
        public GraphInPin Color { get; } = new GraphInPin("color", VariantType.Color) { Value = "1 1 1 1" };
        public GraphInPin Rotation { get; } = new GraphInPin("rotation", VariantType.Float) { Value = "0" };

        public string Material
        {
            get => _material.Value.Path;
            set => _material.Value = new ResourceRef("Material", value);
        }

        public int Columns
        {
            get => _columns.Value;
            set => _columns.Value = value;
        }

        public int Rows
        {
            get => _rows.Value;
            set => _rows.Value = value;
        }
    }
}