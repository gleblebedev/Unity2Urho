using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D.ParticleGraph
{
    public class ParticleGraphEffect
    {
        private readonly List<ParticleGraphLayer> m_layers = new List<ParticleGraphLayer>();

        public List<ParticleGraphLayer> Layers => m_layers;

        public void Write(XmlWriter writer)
        {
            writer.WriteStartElement("particleGraphEffect");
            foreach (var layer in Layers)
            {
                layer.Write(writer);
            }
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteEndElement();
        }
    }
}