using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class GraphNode
    {
        private readonly List<GraphInPin> m_in = new List<GraphInPin>();
        private readonly List<GraphOutPin> m_out = new List<GraphOutPin>();
        private readonly List<GraphEnterPin> m_enter = new List<GraphEnterPin>();
        private readonly List<GraphExitPin> m_exit = new List<GraphExitPin>();
        private readonly List<GraphNodeProperty> m_properties = new List<GraphNodeProperty>();

        public GraphNode(string name, params IGraphElement[] pins)
        {
            Name = name;
            foreach (IGraphElement graphPin in pins)
            {
                if (graphPin is GraphInPin inPin)
                {
                    inPin.Node = this;
                    m_in.Add(inPin);
                }
                else if (graphPin is GraphOutPin outPin)
                {
                    outPin.Node = this;
                    m_out.Add(outPin);
                }
                else if (graphPin is GraphEnterPin enterPin)
                {
                    enterPin.Node = this;
                    m_enter.Add(enterPin);
                }
                else if (graphPin is GraphExitPin exitPin)
                {
                    exitPin.Node = this;
                    m_exit.Add(exitPin);
                }
                else if (graphPin is GraphNodeProperty property)
                {
                    m_properties.Add(property);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Unknown pin or property type " + graphPin.GetType().Name);
                }
            }
        }

        public uint Id { get; set; }
        public string Name { get; set; }

        public List<GraphInPin> In => m_in;
        public List<GraphOutPin> Out => m_out;
        public List<GraphEnterPin> Enter => m_enter;
        public List<GraphExitPin> Exit => m_exit;
        public List<GraphNodeProperty> Properties => m_properties;
        
        public void Write(XmlWriter writer)
        {
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteWhitespace("\t\t");
            writer.WriteStartElement("node");
            writer.WriteAttributeString("id", Id.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("name", Name);
            WriteCollection(writer, "properties", m_properties);
            WriteCollection(writer, "in", m_in);
            WriteCollection(writer, "out", m_out);
            WriteCollection(writer, "enter", m_enter);
            WriteCollection(writer, "exit", m_exit);
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteWhitespace("\t\t");
            writer.WriteEndElement();
        }

        private void WriteCollection<T>(XmlWriter writer, string name, List<T> values) where T: IGraphElement
        {
            if (values.Count > 0)
            {
                writer.WriteWhitespace(Environment.NewLine);
                writer.WriteWhitespace("\t\t\t");
                writer.WriteStartElement(name);
                foreach (var property in values)
                {
                    property.Write(writer);
                }
                writer.WriteWhitespace(Environment.NewLine);
                writer.WriteWhitespace("\t\t\t");
                writer.WriteEndElement();
            }
        }
    }
}