using System;
using System.Globalization;
using System.Xml;

namespace UnityToCustomEngineExporter.Editor.Urho3D.Graph
{
    public class GraphNodeProperty<T> : GraphNodeProperty
    {
        public GraphNodeProperty(string name, T value = default(T))
            : base(name, GetDefaultType())
        {
            Value = value;
        }

        private static VariantType GetDefaultType()
        {
            var f = ValueFormatter<T>.Default;
            if (f == null)
                throw new NotImplementedException($"Formatter is not implemented for type {typeof(T).Name}");
            return f.Type;
        }

        public T Value { get; set; }
        protected override void WriteValue(XmlWriter writer)
        {
            ValueFormatter<T>.Default.WriteValue(writer, Value);
        }
    }

    public abstract class GraphNodeProperty: IGraphElement
    {
        public GraphNodeProperty(string name, VariantType type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }

        public VariantType Type { get; }

        public void Write(XmlWriter writer)
        {
            writer.WriteWhitespace(Environment.NewLine);
            writer.WriteWhitespace("\t\t\t\t");
            writer.WriteStartElement("property");
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("type", string.Format(CultureInfo.InvariantCulture, "{0}", Type));
            WriteValue(writer);
            writer.WriteEndElement();
        }

        protected abstract void WriteValue(XmlWriter writer);

        public static GraphNodeProperty<T> Make<T>(string name, T value)
        {
            return new GraphNodeProperty<T>(name, value);
        }
    }
}