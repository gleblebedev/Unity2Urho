using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class GraphNodeProperty: IGraphElement
    {
        private GraphCurve _curve;
        public GraphNodeProperty(string name, VariantType type)
        {
            Name = name;
            Type = type;
        }
        public GraphNodeProperty(string name, AnimationCurve value, float scale = 1.0f)
            : this(name, VariantType.VariantCurve)
        {
            _curve = new GraphCurve(value, scale);
        }
        public GraphNodeProperty(string name, float value)
            :this(name, VariantType.Float)
        {
            Value = string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }
        public GraphNodeProperty(string name, int value)
            : this(name, VariantType.Int)
        {
            Value = string.Format(CultureInfo.InvariantCulture, "{0}", value);
        }

        public string Name { get; }

        public VariantType Type { get; }

        public string Value { get; set; }

        public void Write(XmlWriter writer)
        {
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteWhitespace("\t\t\t\t");
            writer.WriteStartElement("property");
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("type", string.Format(CultureInfo.InvariantCulture, "{0}", Type));
            if (_curve != null)
            {
                _curve.Write(writer);
            }
            else
            {
                writer.WriteAttributeString("value", Value);
            }
            writer.WriteEndElement();
        }
    }
}