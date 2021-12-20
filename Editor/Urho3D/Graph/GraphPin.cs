using System;
using System.Xml;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public abstract class GraphPin: IGraphElement
    {
        public string Name { get; set; }

        public GraphNode Node { get; internal set; }

        public GraphPin(string name)
        {
            Name = name;
        }

        public void Write(XmlWriter writer)
        {
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteWhitespace("\t\t\t\t");
            writer.WriteStartElement("pin");
            writer.WriteAttributeString("name", Name);
            WriteAttributes(writer);
            writer.WriteEndElement();
        }

        public abstract void WriteAttributes(XmlWriter writer);
    }
}