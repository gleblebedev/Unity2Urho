using System.Xml;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class GraphOutPin : GraphPin
    {
        public GraphOutPin(string name, VariantType type = VariantType.None) : base(name)
        {
            Type = type;
        }

        public VariantType Type { get; set; }

        public override void WriteAttributes(XmlWriter writer)
        {
            if (Type != VariantType.None)
            {
                writer.WriteAttributeString("type", Type.ToString());
            }
        }
    }
}