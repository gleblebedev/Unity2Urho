using System.Xml;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class GraphExitPin : GraphPin
    {
        public GraphExitPin(string name) : base(name) { }
        public GraphOutPin TargetPin { get; set; }
        public override void WriteAttributes(XmlWriter writer)
        {
            if (TargetPin != null)
            {
                writer.WriteAttributeString("node", TargetPin.Node.Name);
                writer.WriteAttributeString("pin", TargetPin.Name);
            }
        }
    }
}