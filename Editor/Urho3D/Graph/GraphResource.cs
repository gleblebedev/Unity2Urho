using System;
using System.Collections.Generic;
using System.Xml;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class GraphResource
    {
        private readonly HashSet<GraphNode> m_nodes = new HashSet<GraphNode>();

        private HashSet<GraphNode> Nodes => m_nodes;

        public void Write(XmlWriter writer)
        {
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("nodes");

            uint id = 1;
            foreach (var graphNode in Nodes)
            {
                graphNode.Id = id;
                ++id;
            }

            foreach (var graphNode in Nodes)
            {
                graphNode.Write(writer);
            }
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteWhitespace("\t");
            writer.WriteEndElement();

        }

        public GraphNode Add(GraphNode graphNode)
        {
            m_nodes.Add(graphNode);
            return graphNode;
        }
    }
}
