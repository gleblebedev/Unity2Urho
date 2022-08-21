using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;
using Object = System.Object;

namespace UnityToCustomEngineExporter.Editor.Urho3D
{
    public static class ExtensionMethods
    {
        internal static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> map, TKey key)
        {
            if (map.TryGetValue(key, out TValue value))
                return value;
            return default(TValue);
        }

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
            writer.WriteAttribute("value", value);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        public static void WriteParameter(this XmlWriter writer, string name, Vector2 value)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("parameter");
            writer.WriteAttributeString("name", name);
            writer.WriteAttribute("value", value);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        public static void WriteParameter(this XmlWriter writer, string name, Vector3 value)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("parameter");
            writer.WriteAttributeString("name", name);
            writer.WriteAttribute("value", value);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        public static void WriteParameter(this XmlWriter writer, string name, Vector4 value)
        {
            writer.WriteWhitespace("\t");
            writer.WriteStartElement("parameter");
            writer.WriteAttributeString("name", name);
            writer.WriteAttribute("value", value);
            writer.WriteEndElement();
            writer.WriteWhitespace(Environment.NewLine);
        }

        public static void WriteAttribute(this XmlWriter writer, string name, Vector4 value)
        {
            writer.WriteAttributeString(name,
                string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", value.x, value.y, value.z, value.w));
        }

        public static void WriteAttribute(this XmlWriter writer, string name, Vector3 value)
        {
            writer.WriteAttributeString(name,
                string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", value.x, value.y, value.z));
        }

        public static void WriteAttribute(this XmlWriter writer, string name, Vector2 value)
        {
            writer.WriteAttributeString(name, string.Format(CultureInfo.InvariantCulture, "{0} {1}", value.x, value.y));
        }

        public static void WriteAttribute(this XmlWriter writer, string name, float value)
        {
            writer.WriteAttributeString(name, value.ToString(CultureInfo.InvariantCulture));
        }

        public static void WriteAttribute(this XmlWriter writer, string name, int value)
        {
            writer.WriteAttributeString(name, value.ToString(CultureInfo.InvariantCulture));
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

        public static void WriteParameter(this XmlWriter writer, string name, Object value)
        {
            if (value == null)
                return;
            if (value is float floatValue)
            {
                WriteParameter(writer, name, floatValue);
                return;
            }
            if (value is Vector2 vec2Value)
            {
                WriteParameter(writer, name, vec2Value);
                return;
            }
            if (value is Vector3 vec3Value)
            {
                WriteParameter(writer, name, vec3Value);
                return;
            }
            if (value is Vector4 vec4Value)
            {
                WriteParameter(writer, name, vec4Value);
                return;
            }
            if (value is Color colorValue)
            {
                WriteParameter(writer, name, colorValue);
                return;
            }
            if (value is Color32 color32Value)
            {
                WriteParameter(writer, name, color32Value);
                return;
            }
            WriteParameter(writer, name, string.Format(CultureInfo.InvariantCulture, "{0}", value.ToString()));
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