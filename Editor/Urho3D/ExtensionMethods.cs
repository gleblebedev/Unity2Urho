using System;
using System.Xml;

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
    }
}