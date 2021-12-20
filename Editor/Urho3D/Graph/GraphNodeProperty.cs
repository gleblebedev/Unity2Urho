using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class GraphNodeProperty: IGraphElement
    {
        private AnimationCurve _curve;
        public GraphNodeProperty(string name, VariantType type)
        {
            Name = name;
            Type = type;
        }
        public GraphNodeProperty(string name, AnimationCurve value, float scale = 1.0f)
            : this(name, VariantType.VariantCurve)
        {
            _curve = value;
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
                writer.WriteWhitespace(Environment.NewLine);
                writer.WriteWhitespace("\t\t\t\t\t");
                writer.WriteStartElement("value");
                writer.WriteAttributeString("name", Name);
                writer.WriteAttributeString("type", VariantType.Float.ToString());
                writer.WriteAttributeString("interpolation", "linear");
                writer.WriteAttributeString("splineTension", "0.5");
                {
                    writer.WriteStartElement("keyframes");
                    foreach (var key in _curve.keys)
                    {
                        writer.WriteWhitespace(Environment.NewLine);
                        writer.WriteWhitespace("\t\t\t\t\t");
                        writer.WriteStartElement("keyframe");
                        writer.WriteAttributeString("time", string .Format(CultureInfo.InvariantCulture, "{0}",key.time));
                        writer.WriteAttributeString("value", string.Format(CultureInfo.InvariantCulture, "{0}", key.value));
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteWhitespace(Environment.NewLine);
                writer.WriteWhitespace("\t\t\t\t\t");
                writer.WriteEndElement();
                writer.WriteWhitespace(Environment.NewLine);
                writer.WriteWhitespace("\t\t\t\t");
            }
            else
            {
                writer.WriteAttributeString("value", Value);
            }
            writer.WriteEndElement();
        }
    }
}