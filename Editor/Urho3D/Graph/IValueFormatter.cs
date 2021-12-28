using System.Xml;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public interface IValueFormatter
    {
        VariantType Type {get; }
        void WriteValue(XmlWriter writer, object val);

        string ToString(object val);
    }
}