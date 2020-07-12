using System;
using System.Globalization;
using System.Xml;
using UnityEngine;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public static class ExtensionMethods
    {
        public static void WriteParameter(this XmlWriter writer, string name, string value)
        {
            writer.WriteStartElement("parameter");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("value", value);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        public static void WriteParameter(this XmlWriter writer, string name, float value)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("parameter");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("value", value.ToString(CultureInfo.InvariantCulture));
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        public static void WriteParameter(this XmlWriter writer, string name, Vector2 value)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("parameter");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("value",
                string.Format(CultureInfo.InvariantCulture, "{0} {1}", value.x, value.y));
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        public static void WriteParameter(this XmlWriter writer, string name, Vector3 value)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("parameter");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("value",
                string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", value.x, value.y, value.z));
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        public static void WriteParameter(this XmlWriter writer, string name, Vector4 value)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("parameter");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("value",
                string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", value.x, value.y, value.z, value.w));
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        public static void WriteParameter(this XmlWriter writer, string name, Quaternion value)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("parameter");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("value",
                string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", value.w, value.x, value.y, value.z));
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        public static void WriteParameter(this XmlWriter writer, string name, Color value)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("parameter");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("value",
                string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", value.r, value.g, value.b, value.a));
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        public static void WriteParameter(this XmlWriter writer, string name, Color32 pos)
        {
            WriteParameter(writer, name, (Color) pos);
        }

        public static void WriteElementParameter(this XmlWriter writer, string parameterName, string valueName,
            string value)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement(parameterName);
            writer.WriteAttributeString(valueName, value);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }
    }
}