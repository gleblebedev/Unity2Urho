using System.Xml;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class GraphEnterPin : GraphPin
    {
        public GraphEnterPin(string name) : base(name) { }
        public override void WriteAttributes(XmlWriter writer)
        {
        }
    }
}