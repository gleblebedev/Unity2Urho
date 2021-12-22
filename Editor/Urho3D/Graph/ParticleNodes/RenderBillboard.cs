using System.Globalization;
using UnityToCustomEngineExporter.Editor.Urho3D.Graph;

namespace Assets.Unity2Urho.Editor.Urho3D.Graph.ParticleNodes
{

    public class RenderBillboard : GraphNode
    {
        private GraphNodeProperty m_material = new GraphNodeProperty("Material", VariantType.ResourceRef);
        private GraphNodeProperty m_columns = new GraphNodeProperty("Columns", 1);
        private GraphNodeProperty m_rows = new GraphNodeProperty("Rows", 1);

        public RenderBillboard() : base(GraphNodeType.RenderBillboard)
        {
            Properties.Add(m_material);
            Properties.Add(m_columns);
            Properties.Add(m_rows);
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
            get => m_material.Value;
            set => m_material.Value = value;
        }

        public int Columns
        {
            get => int.Parse(m_columns.Value);
            set => m_columns.Value = string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }

        public int Rows
        {
            get => int.Parse(m_rows.Value);
            set => m_rows.Value = string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }
    }
}